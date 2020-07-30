using DBD_API.Modules.DbD;
using DBD_API.Modules.DbD.PakItems;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DBD_API.Modules.DbD.JsonResponse;
using SteamKit2.GC.Underlords.Internal;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace DBD_API.Services
{
    public class DdbService
    {
        // static
        private const string UserAgent = "DeadByDaylight/++DeadByDaylight+Live-CL-296874 Windows/10.0.18363.1.256.64bit";

        public static readonly string[] AllowedPrefixes =
        {
            "live",
            "ptb",
            "stage",
            "dev",
            "qa",
            "cert"
        };

        public ConcurrentDictionary<string, ConcurrentDictionary<string, MapInfo>> MapInfos;
        public ConcurrentDictionary<string, ConcurrentDictionary<string, PerkInfo>> PerkInfos;
        public ConcurrentDictionary<string, ConcurrentDictionary<string, OfferingInfo>> OfferingInfos;
        public ConcurrentDictionary<string, ConcurrentDictionary<string, CustomItemInfo>> CustomItemInfos;
        public ConcurrentDictionary<string, ConcurrentDictionary<string, CharacterInfo>> CharacterInfos;
        public ConcurrentDictionary<string, ConcurrentDictionary<string, ItemAddonInfo>> ItemAddonInfos;
        public ConcurrentDictionary<string, ConcurrentDictionary<string, BaseItem>> ItemInfos;
        public ConcurrentDictionary<string, TunableContainer> TunableInfos;

        private readonly IConfiguration _config;
        private readonly ILogger<DdbService> _logger;
        
        private CookieContainer _cookieJar;
        private CacheService _cacheService;

        private Dictionary<string, RestClient> _restClients;
        private Dictionary<string, RestClient> _restCdnClients;
        
        private string _dbdVersion;

        public DdbService(
            ILogger<DdbService> logger,
            CacheService cacheService,
            IConfiguration config
        )
        {
            _cacheService = cacheService;
            _logger = logger;
            _config = config;

            _dbdVersion = null;
            _cookieJar = new CookieContainer();
            
            _restClients = new Dictionary<string, RestClient>();
            _restCdnClients = new Dictionary<string, RestClient>();

            // cached item info
            MapInfos = new ConcurrentDictionary<string, ConcurrentDictionary<string, MapInfo>>();
            PerkInfos = new ConcurrentDictionary<string, ConcurrentDictionary<string, PerkInfo>>();
            OfferingInfos = new ConcurrentDictionary<string, ConcurrentDictionary<string, OfferingInfo>>();
            CustomItemInfos = new ConcurrentDictionary<string, ConcurrentDictionary<string, CustomItemInfo>>();
            CharacterInfos = new ConcurrentDictionary<string, ConcurrentDictionary<string, CharacterInfo>>();
            ItemAddonInfos = new ConcurrentDictionary<string, ConcurrentDictionary<string, ItemAddonInfo>>();
            ItemInfos = new ConcurrentDictionary<string, ConcurrentDictionary<string, BaseItem>>();
            TunableInfos = new ConcurrentDictionary<string, TunableContainer>();


            foreach (var api in AllowedPrefixes)
            {
                _restClients[api] = CreateDBDRestClient($"steam.{api}.bhvrdbd.com");
                _restCdnClients[api] = CreateDBDRestClient($"cdn.{api}.dbd.bhvronline.com");
            }
        }

        private RestClient CreateDBDRestClient(string baseUrl)
            => new RestClient($"https://{baseUrl}")
            {
                UserAgent = UserAgent,
                CookieContainer = _cookieJar
            };

        private static string InvalidResponseJson(string reason)
        {
            try
            {
                return JsonConvert.SerializeObject(new { error = reason, success = "false" });
            } catch (Exception e)
            {
                return string.Format("Json Serailize Error: {0}", e.Message);
            }
        }



        private async Task<IRestResponse> LazyHttpRequest(RestRequest request, string branch, bool cache = true,
            int ttl = 1800)
        {

            var retries = 0;
            IRestResponse response = null;
   
            if (cache)
            {
                var cacheResult = await _cacheService.GetCachedRequest(request, branch);
                if (cacheResult != null) return cacheResult;
            }

            while (true)
            {
                if (retries > 3)
                    break;

                response = await _restClients[branch].ExecuteTaskAsync(request);
                if (response.StatusCode == HttpStatusCode.Forbidden)
                    await UpdateSessionToken(branch);
                else
                    break;

                retries += 1;
            }

            if (cache) 
                await _cacheService.CacheRequest(request, response, branch, ttl);

#if DEBUG
            if (response != null)
            {
                _logger.LogDebug("Request: {0} {1} -> {2}", request.Method.ToString(),
                    request.Resource, response.StatusCode);

                if(response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("Response (NOT OK) ->\n{0}", response.Content);
                }
            }
#endif

            return response;
        }

        private string UpdateDbdVersion(string branch = "live", string config = "", bool fromMainConfig = true)
        {
            if (string.IsNullOrEmpty(config))
                return null;

            try
            {
                JObject versions;

                if (fromMainConfig)
                {
                    var configData = JArray.Parse(config);
                    versions = configData.Children<JObject>()
                        .FirstOrDefault(x => x.GetValue("keyName").ToString() == "VER_CLIENT_DATA");
                }
                else
                {
                    var versionData = JObject.Parse(config);
                    versions = (JObject)versionData["availableVersions"];
                }

                if (versions == null || versions.Equals(default))
                    return null;


                var version = "";
                foreach (KeyValuePair<string, JToken> versionKV in fromMainConfig ? (JObject)versions["value"] : versions)
                {
                    if (versionKV.Key.StartsWith("m_"))
                        break;

                    version = versionKV.Key;
                }

                if(fromMainConfig) 
                    _dbdVersion = version;

                return version;
            }
            catch
            {
                // failed
            }

            return null;
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

        public async Task<string> GetCurrentDBDVersion(string branch = "live")
        {
            RestClient client;
            if (!_restClients.TryGetValue(branch, out client))
                return null;

            var verRequest = new RestRequest("api/v1/utils/contentVersion/version", Method.GET); 
            verRequest.AddHeader("User-Agent", UserAgent);

            var verResponse = await client.ExecuteGetTaskAsync(verRequest);
            if (verResponse.StatusCode != HttpStatusCode.OK)
                return null;

            return UpdateDbdVersion(branch, verResponse.Content, false);
        }

        public async Task<bool> UpdateSessionToken(string branch = "live")
        {
            //var token = await GetSteamSessionToken();
            //if (string.IsNullOrEmpty(token)) return false;
            if (!_restClients.ContainsKey(branch))
                return false;

            var version = await GetCurrentDBDVersion(branch);
            if (string.IsNullOrEmpty(version))
                return false;


            var request = new RestRequest("api/v1/auth/login/guest", Method.POST);
            //var request = new RestRequest("api/v1/auth/provider/steam/login");
            // request.AddQueryParameter("token", token);
            request.AddJsonBody(new
            {
                clientData = new
                {
                    catalogId = version,
                    gameContentId = version,
                    consentId = version
                }
            });

            var response = await _restClients[branch].ExecutePostTaskAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
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
            if (!_restClients.ContainsKey(branch))
                return null;

            var restRequest = new RestRequest("api/v1/extensions/shrine/getAvailable", Method.POST);
            restRequest.AddJsonBody(new {data = new {version = "steam"}});

            var response = await LazyHttpRequest(restRequest, branch, false) ;
            var body = Encoding.ASCII.GetString(response.RawBytes);
            return response.StatusCode == HttpStatusCode.OK ? ShrineConvert.FromJson(body) : null;
        }

        public async Task<StoreResponse> GetStoreOutfits(string branch = "live")
        {
            if (!_restClients.ContainsKey(branch))
                return null;

            var restRequest = new RestRequest("api/v1/extensions/store/getOutfits", Method.POST);
            restRequest.AddJsonBody(new
            {
                data = new { }
            });

            var response = await LazyHttpRequest(restRequest, branch);
            var body = Encoding.ASCII.GetString(response.RawBytes);
            return response.StatusCode == HttpStatusCode.OK ? StoreConvert.FromJson(body) : null;
        }

        public async Task<string> GetApiConfig(string branch = "live")
        {
            if (!_restClients.ContainsKey(branch))
                return InvalidResponseJson("Disallowed api branch!");

            var restRequest = new RestRequest("api/v1/config", Method.GET);

            var response = await LazyHttpRequest(restRequest, branch, false);
            var responseBody = response.StatusCode == HttpStatusCode.OK ? 
                Encoding.ASCII.GetString(response.RawBytes) : InvalidResponseJson("Invalid response from DBD Api");

            if (branch == "live" && response.StatusCode == HttpStatusCode.OK)
                UpdateDbdVersion(branch, responseBody);

            return responseBody;
        } 

        public async Task<string> GetCdnContentFormat(string uriFormat, string cdnPrefix, bool cache = true)
        {
            if (_dbdVersion == null && (await GetApiConfig()).Contains("Invalid response from DBD Api"))
                return InvalidResponseJson("Failed to get DBD version");

            return await GetCdnContent(string.Format(uriFormat, _dbdVersion), cdnPrefix, cache);
        }

        public async Task<string> GetCdnContent(string uri, string cdnPrefix = "live", bool cache = true)
        {
            if (string.IsNullOrEmpty(_config["dbd_decrypt_key"]))
                return InvalidResponseJson("Invalid decryption key!");

            if (!_restCdnClients.ContainsKey(cdnPrefix))
                return InvalidResponseJson("Disallowed cdn branch!");

            var request = new RestRequest(uri);
            IRestResponse response;

            if (cache)
            {
                response = await _cacheService.GetCachedRequest(request, cdnPrefix);
                if (response != null) goto skipOver;
            }
            
            response = await _restCdnClients[cdnPrefix].ExecuteGetTaskAsync(request);
            if (cache) await _cacheService.CacheRequest(request, response, cdnPrefix);

        skipOver:
            if (response == null || !response.IsSuccessful)
                return InvalidResponseJson("Invalid response from DBD CDN");

            var body = Encoding.UTF8.GetString(response.RawBytes, 0, (int)response.ContentLength);

            return response.StatusCode != HttpStatusCode.OK ? "" : Encryption.DecryptCdn(body, _config["dbd_decrypt_key"]);
        }

    }
}
