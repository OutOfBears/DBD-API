// credit: jesterret (b1g credit)
// saved me a bunch of money with steamkit suggestion + example :)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using DBD_API.Modules.Steam;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SteamKit2;
using SteamKit2.Internal;
using EResult = SteamKit2.EResult;
using SteamApps = SteamKit2.SteamApps;
using SteamClient = SteamKit2.SteamClient;
using SteamUser = SteamKit2.SteamUser;


namespace DBD_API.Services
{
    public class SteamService
    {
        private readonly IConfiguration _config;

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

        public SteamService(IConfiguration config)
        {
            Connected = false;
            KeepAlive = true;
            _config = config;

            _gcTokens = new ConcurrentQueue<byte[]>();
            _gcTokensComplete = new TaskCompletionSource<bool>();
            _gameTickets = new ConcurrentDictionary<uint, List<CMsgAuthTicket>>();

            InitializeClient();
        }

        private void InitializeClient()
        {
            // client/manager creation
            Client = new SteamClient();
            Manager = new CallbackManager(Client);

            _user = Client.GetHandler<SteamUser>();
            _apps = Client.GetHandler<SteamApps>();

            Client.AddHandler(new SteamTicketAuth());

            // subscriptions
            Manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            Manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            Manager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth);
            Manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);


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

            var authList = new ClientMsgProtobuf<CMsgClientAuthList>(EMsg.ClientAuthList, 64);
            authList.Body.tokens_left = (uint) _gcTokens.Count;
            authList.Body.app_ids.AddRange(_gameTickets.Keys);
            authList.Body.tickets.AddRange(_gameTickets.Values.SelectMany(x => x));
            authList.SourceJobID = Client.GetNextJobID();
            Client.Send(authList);

            return new AsyncJob<SteamTicketAccepted>(Client, authList.SourceJobID);
        }

        // events
        private void OnGcTokens(SteamApps.GameConnectTokensCallback obj)
        {
            foreach(var token in obj.Tokens) _gcTokens.Enqueue(token);
            while (_gcTokens.Count > obj.TokensToKeep) _gcTokens.TryDequeue(out _);
            _gcTokensComplete.TrySetResult(true);
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback obj)
        {
            if (obj.Result != EResult.OK)
            {
                Console.WriteLine("[Steam] Login denied, reason: {0}", obj.Result);
                Client.Disconnect();
                return;
            }

            Connected = true;
            Console.WriteLine("[Steam] We are connected! (user={0})", _config["steam_user"]);
        }

        public void OnMachineAuth(SteamUser.UpdateMachineAuthCallback obj)
        {
            Console.WriteLine("[Steam] Writing sentry-file...");
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

            Console.WriteLine("[Steam] Finished writing sentry-file!");
        }

        private void OnConnected(SteamClient.ConnectedCallback obj)
        {
            Console.WriteLine("[Steam] Connected to steam! Logging in as '{0}'...",
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
            Console.WriteLine("[Steam] Disconnected from steam!");
            // Console.WriteLine(Environment.StackTrace);
            Connected = false;

            if (!obj.UserInitiated)
                Client.Connect();
            else
                KeepAlive = false;
        }

    }
}
