// credit: jesterret (b1g credit)
// saved me a bunch of money with steamkit suggestion + example :)

using DBD_API.Modules.Steam;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SteamKit2;
using SteamKit2.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using EResult = SteamKit2.EResult;
using LicenseList = System.Collections.Generic.List<SteamKit2.SteamApps.LicenseListCallback.License>;
using SteamApps = SteamKit2.SteamApps;
using SteamClient = SteamKit2.SteamClient;
using SteamUser = SteamKit2.SteamUser;


namespace DBD_API.Services
{
    public class SteamService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SteamService> _logger;

        public readonly TaskCompletionSource<bool> LicenseCompletionSource;
        public LicenseList Licenses;
        public bool Connected;
        public bool KeepAlive;

        // token grabbing
        private ConcurrentQueue<byte[]> _gcTokens;
        private TaskCompletionSource<bool> _gcTokensComplete;
        private ConcurrentDictionary<uint, List<CMsgAuthTicket>> _gameTickets;

        // steamkit required
        public CallbackManager Manager;
        public SteamClient Client;
        private SteamUser _user;
        private SteamApps _apps;
        private SteamUserStats _userStats;

        public SteamService(IConfiguration config, ILogger<SteamService> logger)
        {
            Connected = false;
            KeepAlive = true;
            _config = config;
            _logger = logger;

            _gcTokens = new ConcurrentQueue<byte[]>();
            _gcTokensComplete = new TaskCompletionSource<bool>();
            _gameTickets = new ConcurrentDictionary<uint, List<CMsgAuthTicket>>();

            Licenses = new LicenseList();
            LicenseCompletionSource = new TaskCompletionSource<bool>(false);

            InitializeClient();
        }

        private void InitializeClient()
        {
            // client/manager creation
            Client = new SteamClient();
            Manager = new CallbackManager(Client);

            _user = Client.GetHandler<SteamUser>();
            _apps = Client.GetHandler<SteamApps>();
            _userStats = Client.GetHandler<SteamUserStats>();

            Client.AddHandler(new SteamTicketAuth());
            Client.AddHandler(new SteamStatsHandler());

            // subscriptions
            Manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            Manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            Manager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth);
            Manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            Manager.Subscribe<SteamApps.LicenseListCallback>(OnLicenseList);

            // internal subs
            Manager.Subscribe<SteamApps.GameConnectTokensCallback>(OnGcTokens);
        }

        // methods
        public async Task<byte[]> GetAuthSessionTicket(GameID gameId)
        {
            if (!Connected)
                throw new Exception("Not connected to stream");

            await _gcTokensComplete.Task;
            if (!_gcTokens.TryDequeue(out var token))
                throw new Exception("Failed to get gc token");

            var ticket = new MemoryStream();
            var writer = new BinaryWriter(ticket);

            CreateAuthTicket(token, writer);

            var verifyTask = VerifyTicket(gameId.AppID, ticket, out uint crc).ToTask();
            var appTicketTask = _apps.GetAppOwnershipTicket(gameId.AppID).ToTask();
            await Task.WhenAll(verifyTask, appTicketTask);

            var callback = await verifyTask;
            if (callback.ActiveTicketsCRC.All(x => x != crc))
                throw new Exception("Failed verify active tickets");

            var appTicket = await appTicketTask;
            if (appTicket.Result != EResult.OK)
                throw new Exception("Failed to get ownership ticket");

            writer.Write(appTicket.Ticket.Length);
            writer.Write(appTicket.Ticket);
            return ticket.ToArray();

        }


        // TODO: implement me
        public async Task GetUserAchievements(GameID gameId, SteamID steamId)
        {
            var response = await RequestUserStats(gameId, steamId);
            //if (response.Result != EResult.OK || response.AchievementBlocks == null)
            //return null;

            //return response.AchievementBlocks;
        }

        public async Task<Dictionary<string, object>> GetUserStats(GameID gameId, SteamID steamId)
        {
            var response = await RequestUserStats(gameId, steamId);
            if (response.Result != EResult.OK || response.Schema == null)
                return null;

            var results = new Dictionary<string, object>();
            var schema = response.ParsedSchema;
            var stats = schema.Children.FirstOrDefault(x => x.Name == "stats");
            if (stats == null || stats.Equals(default(Dictionary<string, double>)))
                goto exit;

            foreach (var stat in stats.Children)
            {
                var statName = stat.Children.FirstOrDefault(x => x.Name == "name");
                if (statName == null || statName.Equals(default(KeyValue)))
                    continue;

                
                var statValue = response.Stats.FirstOrDefault(x => Convert.ToString(x.stat_id) == stat.Name);
                if (statValue == null || statValue.Equals(default(CMsgClientGetUserStatsResponse.Stats)))
                    continue;

                if(statName.Value.EndsWith("_float"))
                    results.TryAdd(statName.Value, BitConverter.ToSingle(BitConverter.GetBytes(statValue.stat_value)));
                else
                    results.TryAdd(statName.Value, statValue.stat_value);
            }

        exit:
            return results;
        }

        // internals
        private void CreateAuthTicket(byte[] gcToken, BinaryWriter stream)
        {
            // gc token
            stream.Write(gcToken.Length);
            stream.Write(gcToken.ToArray());

            // session
            stream.Write(24);   // length
            stream.Write(1);    // unk 1
            stream.Write(2);    // unk 2
            stream.Write(0);    // pub ip addr
            stream.Write(0);    // padding
            stream.Write(2038); // ms connected
            stream.Write(1);    // connection count
        }

        private AsyncJob<SteamTicketAccepted> VerifyTicket(uint appId, MemoryStream stream, out uint crc)
        {
            var authTicket = stream.ToArray();
            crc = BitConverter.ToUInt32(CryptoHelper.CRCHash(authTicket));
            var items = _gameTickets.GetOrAdd(appId, new List<CMsgAuthTicket>());
            items.Add(new CMsgAuthTicket()
            {
                gameid = appId,
                ticket = authTicket,
                ticket_crc = crc
            });

            var authList = new ClientMsgProtobuf<CMsgClientAuthList>(EMsg.ClientAuthList);
            authList.Body.tokens_left = (uint)_gcTokens.Count;
            authList.Body.app_ids.AddRange(_gameTickets.Keys);
            authList.Body.tickets.AddRange(_gameTickets.Values.SelectMany(x => x));
            authList.SourceJobID = Client.GetNextJobID();
            Client.Send(authList);

            return new AsyncJob<SteamTicketAccepted>(Client, authList.SourceJobID);
        }

        private AsyncJob<SteamUserStatsResponse> RequestUserStats(GameID appId, SteamID steamId)
        {
            var getUserStats = new ClientMsgProtobuf<CMsgClientGetUserStats>(EMsg.ClientGetUserStats);
            getUserStats.Body.game_id = appId;
            getUserStats.Body.game_idSpecified = true;
            getUserStats.Body.steam_id_for_user = steamId;
            getUserStats.Body.steam_id_for_userSpecified = true;

            getUserStats.SourceJobID = Client.GetNextJobID();
            Client.Send(getUserStats);

            return new AsyncJob<SteamUserStatsResponse>(Client, getUserStats.SourceJobID);
        }

        // events
        private void OnGcTokens(SteamApps.GameConnectTokensCallback obj)
        {
            foreach (var token in obj.Tokens) _gcTokens.Enqueue(token);
            while (_gcTokens.Count > obj.TokensToKeep) _gcTokens.TryDequeue(out _);
            _gcTokensComplete.TrySetResult(true);
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback obj)
        {
            if (obj.Result != EResult.OK)
            {
                _logger.LogError("Login denied, reason: {0}", obj.Result);
                Client.Disconnect();
                return;
            }

            Connected = true;
            _logger.LogInformation("We are connected! (user={0})", _config["steam_user"]);
        }

        public void OnMachineAuth(SteamUser.UpdateMachineAuthCallback obj)
        {
            _logger.LogInformation("Writing sentry-file...");
            int sentrySize;
            byte[] sentryHash;


            using (var fs = File.Open("sentry.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                fs.Seek(obj.Offset, SeekOrigin.Begin);
                fs.Write(obj.Data, 0, obj.BytesToWrite);
                sentrySize = (int)fs.Length;
                fs.Seek(0, SeekOrigin.Begin);
                using (var sha = SHA1.Create())
                    sentryHash = sha.ComputeHash(fs);
            }

            _user.SendMachineAuthResponse(new SteamUser.MachineAuthDetails()
            {
                JobID = obj.JobID,
                FileName = obj.FileName,
                BytesWritten = obj.BytesToWrite,
                FileSize = sentrySize,
                Offset = obj.Offset,
                Result = EResult.OK,
                LastError = 0,
                OneTimePassword = obj.OneTimePassword,
                SentryFileHash = sentryHash
            });

            _logger.LogInformation("Finished writing sentry-file!");
        }

        private void OnConnected(SteamClient.ConnectedCallback obj)
        {
            _logger.LogInformation("Connected to steam! Logging in as '{0}'...",
                _config["steam_user"]);

            byte[] sentryHash = null;
            if (File.Exists("sentry.bin"))
                sentryHash = CryptoHelper.SHAHash(
                    File.ReadAllBytes("sentry.bin")
                );

            _user.LogOn(new SteamUser.LogOnDetails()
            {
                ShouldRememberPassword = true,
                Username = _config["steam_user"],
                Password = _config["steam_pass"],
                TwoFactorCode = _config["steam_mfa"],
                AuthCode = _config["steam_oauth"],
                SentryFileHash = sentryHash,
            });
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback obj)
        {
            _logger.LogInformation("Disconnected from steam!");
            // Console.WriteLine(Environment.StackTrace);
            Connected = false;

            if (!obj.UserInitiated)
                Client.Connect();
            else
                KeepAlive = false;
        }


        private void OnLicenseList(SteamApps.LicenseListCallback obj)
        {
            if (obj.Result != EResult.OK) return;

            Licenses.Clear();
            Licenses.AddRange(obj.LicenseList);

            if(!LicenseCompletionSource.Task.IsCompleted)
                LicenseCompletionSource.TrySetResult(true);
        }

    }
}
