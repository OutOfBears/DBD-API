// Based on: https://github.com/SteamRE/DepotDownloader/blob/master/DepotDownloader/CDNClientPool.cs

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

using CDNAuthTokenCallback = SteamKit2.SteamApps.CDNAuthTokenCallback;

namespace DBD_API.Modules.Steam.ContentManagement
{
    class CDNClientPool
    {
        private const int ServerEndpointMinimumSize = 8;

        private SteamClient _client;
        private SteamApps _apps;

        private readonly ConcurrentBag<CDNClient> _activeClientPool;
        private readonly ConcurrentDictionary<string, uint> _contentServerPenalty;
        private readonly ConcurrentDictionary<CDNClient, Tuple<uint, CDNClient.Server>> _activeClientAuthed;
        private readonly BlockingCollection<CDNClient.Server> _availableServerEndpoints;
        private readonly CancellationTokenSource _cancellation;

        private ConcurrentDictionary<string, CDNAuthTokenCallback> _cdnAuthTokens;
        private AutoResetEvent _populateEvent;


        public CDNClientPool(SteamClient client, CancellationTokenSource cancellationToken)
        {
            // to be supplied by the background service
            _cancellation = cancellationToken;
            _client = client;
            _apps = client.GetHandler<SteamApps>();


            _populateEvent = new AutoResetEvent(true);
            _activeClientPool = new ConcurrentBag<CDNClient>();
            _contentServerPenalty = new ConcurrentDictionary<string, uint>();
            _activeClientAuthed = new ConcurrentDictionary<CDNClient, Tuple<uint, CDNClient.Server>>();
            _availableServerEndpoints = new BlockingCollection<CDNClient.Server>();
            _cdnAuthTokens = new ConcurrentDictionary<string, CDNAuthTokenCallback>();

            Task.Factory.StartNew(ConnectionPoolMonitor, cancellationToken.Token).Unwrap();
        }

        public async Task<IReadOnlyCollection<CDNClient.Server>> FetchServerList()
        {
            while (!_cancellation.IsCancellationRequested)
            {
                try
                {
                    var cdnServers =
                        await ContentServerDirectoryService.LoadAsync(_client.Configuration, _cancellation.Token);
                    if (cdnServers != null)
                        return cdnServers;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to grab content server list");
                }
            }

            return null;
        }

        public async Task ConnectionPoolMonitor()
        {
            while (!_cancellation.IsCancellationRequested)
            {
                _populateEvent.WaitOne(TimeSpan.FromSeconds(1));

                if (_availableServerEndpoints.Count >= ServerEndpointMinimumSize || !_client.IsConnected)
                    continue;

                var servers = await FetchServerList();
                if (servers == null)
                {
                    return;
                }

                var weightedServers = servers.Select(x =>
                {
                    _contentServerPenalty.TryGetValue(x.Host, out var penalty);
                    return Tuple.Create(x, penalty);
                })
                    .OrderBy(x => x.Item2)
                    .ThenBy(x => x.Item1.WeightedLoad);

                foreach (var endpoint in weightedServers)
                    for (var i = 0; i < endpoint.Item1.NumEntries; i++)
                        _availableServerEndpoints.Add(endpoint.Item1);
            }
        }

        public async Task<CDNClient> GetConnectionForDepotAsync(uint appId, uint depotId, byte[] depotKey,
            CancellationToken token)
        {
            CDNClient client = null;
            Tuple<uint, CDNClient.Server> authData;
            _activeClientPool.TryTake(out client);

            if (client == null)
                client = await BuildConnection(appId, depotId, depotKey, null, token);

            if (!_activeClientAuthed.TryGetValue(client, out authData) || authData.Item1 != depotId)
            {
                if (authData?.Item2.Type == "CDN" || authData?.Item2.Type == "SteamCache")
                {
                    Console.WriteLine("Re-authed CDN connection to content server {0} from {1} to {2}", authData.Item2, authData.Item1, depotId);
                    await AuthenticateConnection(client, authData.Item2, appId, depotId, depotKey);
                }
                else if (authData?.Item2.Type == "CS")
                {
                    Console.WriteLine("Re-authed anonymous connection to content server {0} from {1} to {2}", authData.Item2, authData.Item1, depotId);
                    await AuthenticateConnection(client, authData.Item2, appId, depotId, depotKey);
                }
                else
                {
                    client = await BuildConnection(appId, depotId, depotKey, authData?.Item2, token);
                }
            }

            return client;

        }


        public void ReturnConnection(CDNClient client)
        {
            if (client == null) return;
            _activeClientPool.Add(client);
        }

        public void ReturnBrokenConnection(CDNClient client)
        {
            if (client == null) return;
            ReleaseConnection(client);
        }

        private void ReleaseConnection(CDNClient client)
        {
            Tuple<uint, CDNClient.Server> authData;
            _activeClientAuthed.TryRemove(client, out authData);
        }

        private async Task<bool> AuthenticateConnection(CDNClient client, CDNClient.Server server, uint appId,
            uint depotId, byte[] depotKey)
        {

            try
            {
                string cdnAuthToken = null;

                if (server.Type == "CDN" || server.Type == "SteamCache")
                    cdnAuthToken = (await RequestCDNAuthToken(appId, depotId, server.Host))?.Token;

                await client.AuthenticateDepotAsync(depotId, depotKey, cdnAuthToken);
                _activeClientAuthed[client] = Tuple.Create(depotId, server);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to reauth to content server {0}: {1}", server, ex.Message);
            }

            return false;
        }


        private async Task<CDNClient> BuildConnection(uint appId, uint depotId, byte[] depotKey,
            CDNClient.Server serverSeed, CancellationToken token)
        {
            CDNClient.Server server = null;
            CDNClient client = null;

            while (client == null)
            {
                if (serverSeed != null)
                {
                    server = serverSeed;
                    serverSeed = null;
                }
                else
                {
                    if (_availableServerEndpoints.Count < ServerEndpointMinimumSize)
                        _populateEvent.Set();

                    server = _availableServerEndpoints.Take(token);
                }

                client = new CDNClient(_client);

                try
                {
                    var cdnToken = "";

                    if (server.Type == "CDN" || server.Type == "SteamCache")
                        cdnToken = (await RequestCDNAuthToken(appId, depotId, server.Host))?.Token;

                    await client.ConnectAsync(server);
                    await client.AuthenticateDepotAsync(depotId, depotKey, cdnToken);
                }
                catch (Exception ex)
                {
                    client = null;
                    Console.WriteLine("Failed to connect to content server {0}: {1}", server, ex.Message);
                    _contentServerPenalty.TryGetValue(server.Host, out var penalty);
                    _contentServerPenalty[server.Host] = ++penalty;

                }
            }

            Console.WriteLine("Initialized connection to content server {0} with depot id {1}", server, depotId);
            _activeClientAuthed[client] = Tuple.Create(depotId, server);

            return client;
        }


        private async Task<CDNAuthTokenCallback> RequestCDNAuthToken(uint appId, uint depotId, string host)
        {
            host = ResolveCDNTopLevelHost(host);
            var cdnKey = $"{depotId:D}:{host}";

            if (_cdnAuthTokens.TryGetValue(cdnKey, out CDNAuthTokenCallback callback) && callback != null)
                return callback;

            callback = await _apps.GetCDNAuthToken(appId, depotId, host);
            _cdnAuthTokens[cdnKey] = callback ?? throw new Exception("Failed to get CDN token");
            return callback;
        }

        private static string ResolveCDNTopLevelHost(string host)
        {
            return host.EndsWith(".steampipe.steamcontent.com") ? "steampipe.steamcontent.com" : host;
        }
    }
}
