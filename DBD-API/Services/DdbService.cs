using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

using RestSharp;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Configuration;
using SteamKit2;

namespace DBD_API.Services
{
    public class DdbService
    {
        // static
        private const string ApiHost = "latest.live.dbd.bhvronline.com";

        private const string CdnHost = "cdn.live.dbd.bhvronline.com";

        private readonly IConfiguration _config;

        // not static
        //private SteamService _steamService;
        private CookieContainer _cookieJar;
        private RestClient _restClient;
        private RestClient _cdnRestClient;

        public DdbService(
            //SteamService steamService,
            IConfiguration config
        )
        {
            _config = config;
            //_steamService = steamService;
            _cookieJar = new CookieContainer();

            Func<string, RestClient> initRestClient = host =>
                new RestClient($"https://{host}")
                {
                    UserAgent = "DeadByDaylight/++DeadByDaylight+Live-CL-214681 Windows/10.0.17763.1.256.64bit",
                    CookieContainer = _cookieJar
                };

            _restClient = initRestClient(ApiHost);
            _cdnRestClient = initRestClient(CdnHost);
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

        public async Task<bool> UpdateSessionToken()
        {
            //var token = await GetSteamSessionToken();
            //if (string.IsNullOrEmpty(token)) return false;

            var request = new RestRequest("api/v1/auth/login/guest");
            //var request = new RestRequest("api/v1/auth/provider/steam/login");
            // request.AddQueryParameter("token", token);
            request.AddJsonBody(new
            {
                clientData = new { consentId = "2" }
            });

            var response = await _restClient.ExecutePostTaskAsync(request);
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

        public async Task<JObject> GetShrine()
        {
            var retries = 0;
            while (true)
            {
                if (retries > 3) return null;

                var restRequest = new RestRequest("api/v1/extensions/shrine/getAvailable");
                restRequest.AddJsonBody(new {data = new {version = "steam"}});

                var response = await _restClient.ExecutePostTaskAsync(restRequest);
                if (response.StatusCode == HttpStatusCode.OK)
                    return JObject.Parse(Encoding.ASCII.GetString(response.RawBytes));

                else
                {
                    await UpdateSessionToken();
                    retries += 1;
                }
            }
        }

        public async Task<JObject> GetStoreOutfits()
        {
            var restRequest = new RestRequest("api/v1/extensions/store/getOutfits");
            restRequest.AddJsonBody(new
            {
                data = new { }
            });

            var response = await _restClient.ExecutePostTaskAsync(restRequest);
            return response.StatusCode == HttpStatusCode.OK ? JObject.Parse(Encoding.ASCII.GetString(response.RawBytes)) : null;
        }

        public async Task<string> GetApiConfig()
        {
            var retries = 0;
            while (true)
            {
                if (retries > 3) return null;

                var restRequest = new RestRequest("/api/v1/config");
                var response = await _restClient.ExecuteGetTaskAsync(restRequest);
                if (response.StatusCode == HttpStatusCode.OK)
                    return Encoding.ASCII.GetString(response.RawBytes);

                else
                {
                    await UpdateSessionToken();
                    retries += 1;
                }
            }


        }

        public async Task<string> GetCdnContent(string uri)
        {
            if (string.IsNullOrEmpty(_config["dbd_decrypt_key"]))
                return "";

            var request = new RestRequest(uri);
            var response = await _cdnRestClient.ExecuteGetTaskAsync(request);
            var body = Encoding.UTF8.GetString(response.RawBytes, 0, (int)response.ContentLength);

            return response.StatusCode != HttpStatusCode.OK ? "" : Modules.DbD.Extra.DecryptCdn(body, _config["dbd_decrypt_key"]);
        }

    }
}
