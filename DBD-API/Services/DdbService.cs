using Newtonsoft.Json.Linq;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

using RestSharp;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using DBD_API.Modules.DbD;
using DBD_API.Modules.DbD.Items;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SteamKit2;
using SteamKit2.GC.Dota.Internal;

namespace DBD_API.Services
{
    public class DdbService
    {
        // static

        public static readonly string[] AllowedPrefixes =
        {
            "live",
            "ptb",
            "stage",
            "dev",
            "qa",
            "cert"
        };

        public ConcurrentDictionary<string, ConcurrentDictionary<string, PerkInfo>> PerkInfos;
        public ConcurrentDictionary<string, ConcurrentDictionary<string, OfferingInfo>> OfferingInfos;
        public ConcurrentDictionary<string, ConcurrentDictionary<string, CustomItemInfo>> CustomItemInfos;
        public ConcurrentDictionary<string, ConcurrentDictionary<string, CharacterInfo>> CharacterInfos;
        public ConcurrentDictionary<string, ConcurrentDictionary<string, ItemAddonInfo>> ItemAddonInfos;
        public ConcurrentDictionary<string, ConcurrentDictionary<string, BaseItem>> ItemInfos;

        public ConcurrentDictionary<string, ConcurrentDictionary<string, TunableInfo>> TunableInfos;

        private readonly IConfiguration _config;

        // not static
        //private SteamService _steamService;
        private CookieContainer _cookieJar;
        private Dictionary<string, RestClient> _restClients;
        private Dictionary<string, RestClient> _restCdnClients;

        public DdbService(
            //SteamService steamService,
            IConfiguration config
        )
        {
            _config = config;
            //_steamService = steamService;
            _cookieJar = new CookieContainer();
            
            _restClients = new Dictionary<string, RestClient>();
            _restCdnClients = new Dictionary<string, RestClient>();

            // cached item info
            PerkInfos = new ConcurrentDictionary<string, ConcurrentDictionary<string, PerkInfo>>();
            OfferingInfos = new ConcurrentDictionary<string, ConcurrentDictionary<string, OfferingInfo>>();
            CustomItemInfos = new ConcurrentDictionary<string, ConcurrentDictionary<string, CustomItemInfo>>();
            CharacterInfos = new ConcurrentDictionary<string, ConcurrentDictionary<string, CharacterInfo>>();
            ItemAddonInfos = new ConcurrentDictionary<string, ConcurrentDictionary<string, ItemAddonInfo>>();
            ItemInfos = new ConcurrentDictionary<string, ConcurrentDictionary<string, BaseItem>>();
            TunableInfos = new ConcurrentDictionary<string, ConcurrentDictionary<string, TunableInfo>>();


            foreach (var api in AllowedPrefixes)
            {
                _restClients[api] = CreateDBDRestClient($"latest.{api}.dbd.bhvronline.com");
                _restCdnClients[api] = CreateDBDRestClient($"cdn.{api}.dbd.bhvronline.com");
            }
        }

        private RestClient CreateDBDRestClient(string baseUrl)
            => new RestClient($"https://{baseUrl}")
            {
                UserAgent = "DeadByDaylight/++DeadByDaylight+Live-CL-214681 Windows/10.0.17763.1.256.64bit",
                CookieContainer = _cookieJar
            };

        private string InvalidResponseJson(string reason)
        {
            try
            {
                return JsonConvert.SerializeObject(new { error = reason, success = "false" });
            } catch (Exception e)
            {
                return string.Format("Json Serailize Error: {0}", e.Message);
            }
        }

        /*
        private async Task<string> GetSteamSessionToken()
        {
            if (!_steamService.Connected) return "";

            // dead by daylight app
            byte[] buffer = await _steamService.GetAuthSessionTicket(new GameID(381210));
            var token = BitConverter.ToString(buffer, 0, (int)buffer.Length);
            Console.WriteLine("token {0}", token.Replace("-", string.Empty));
            return token.Replace("-", string.Empty);
        }
        */

        public async Task<bool> UpdateSessionToken(string branch = "live")
        {
            //var token = await GetSteamSessionToken();
            //if (string.IsNullOrEmpty(token)) return false;
            if (!_restClients.ContainsKey(branch))
                return false;

            var request = new RestRequest("api/v1/auth/login/guest");
            //var request = new RestRequest("api/v1/auth/provider/steam/login");
            // request.AddQueryParameter("token", token);
            request.AddJsonBody(new
            {
                clientData = new { consentId = "2" }
            });

            var response = await _restClients[branch].ExecutePostTaskAsync(request);
            if(response.StatusCode == HttpStatusCode.OK)
                foreach(var cookie in response.Cookies)
                {
                    if (cookie.Name != "bhvrSession" || string.IsNullOrEmpty(cookie.Value)) continue;
                    _cookieJar.Add(new Cookie(cookie.Name, cookie.Value)
                    {
                        Domain = cookie.Domain
                    });

                    return true;
                }

            return false;
        }

        public async Task<ShrineResponse> GetShrine(string branch = "live")
        {
            var retries = 0;

            if (!_restClients.ContainsKey(branch))
                return null;

            while (true)
            {
                if (retries > 3) return null;

                var restRequest = new RestRequest("api/v1/extensions/shrine/getAvailable");
                restRequest.AddJsonBody(new {data = new {version = "steam"}});

                var response = await _restClients[branch].ExecutePostTaskAsync(restRequest);
                if (response.StatusCode == HttpStatusCode.OK)
                    return JsonConvert.DeserializeObject<ShrineResponse>(Encoding.ASCII.GetString(response.RawBytes));

                else
                {
                    await UpdateSessionToken(branch);
                    retries += 1;
                }
            }
        }

        public async Task<JObject> GetStoreOutfits(string branch = "live")
        {
            if (!_restClients.ContainsKey(branch))
                return null;

            var restRequest = new RestRequest("api/v1/extensions/store/getOutfits");
            restRequest.AddJsonBody(new
            {
                data = new { }
            });

            var response = await _restClients[branch].ExecutePostTaskAsync(restRequest);
            return response.StatusCode == HttpStatusCode.OK ? JObject.Parse(Encoding.ASCII.GetString(response.RawBytes)) : null;
        }

        public async Task<string> GetApiConfig(string branch = "live")
        {
            if (!_restClients.ContainsKey(branch))
                return InvalidResponseJson("Disallowed api branch!");

            var retries = 0;
            while (true)
            {
                if (retries > 3) return null;

                var restRequest = new RestRequest("/api/v1/config");
                var response = await _restClients[branch].ExecuteGetTaskAsync(restRequest);
                if (response.StatusCode == HttpStatusCode.OK)
                    return Encoding.ASCII.GetString(response.RawBytes);

                else
                {
                    await UpdateSessionToken(branch);
                    retries += 1;
                }
            }
        }

        public async Task<string> GetCdnContent(string uri, string cdnPrefix = "live")
        {
            if (string.IsNullOrEmpty(_config["dbd_decrypt_key"]))
                return InvalidResponseJson("Invalid decryption key!");

            if (!_restCdnClients.ContainsKey(cdnPrefix))
                return InvalidResponseJson("Disallowed cdn branch!");

            var request = new RestRequest(uri);
            var response = await _restCdnClients[cdnPrefix].ExecuteGetTaskAsync(request);
            if (!response.IsSuccessful)
                return InvalidResponseJson("Invalid response from DBD CDN");

            var body = Encoding.UTF8.GetString(response.RawBytes, 0, (int)response.ContentLength);

            return response.StatusCode != HttpStatusCode.OK ? "" : Modules.DbD.Extra.DecryptCdn(body, _config["dbd_decrypt_key"]);
        }

    }
}
