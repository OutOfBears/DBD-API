using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using RestSharp;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Steamworks;

namespace DBD_API.Services
{
    public class DBDService
    {
        // static
        private readonly IConfiguration config;
        private string host;

        // not static
        private CookieContainer cookieJar;
        private RestClient restClient;
        

        public DBDService(
            IConfiguration _config
        )
        {
            this.config = _config;
            this.InitRestAPI();
        }

        private void InitRestAPI()
        {
            host = "latest.live.dbd.bhvronline.com";
       
            this.cookieJar = new CookieContainer();
            this.restClient = new RestClient(string.Format("https://{0}", host))
            {
                UserAgent = "game=DeadByDaylight, engine=UE4, version=4.13.2-0+UE4",
                CookieContainer = cookieJar
            };
        }

        private string GetSTEAMSessionToken()
        {
            byte[] buffer = new byte[1024];
            var status = SteamUser.GetAuthSessionTicket(buffer, buffer.Length, out uint bufferSize);

            if (status != HAuthTicket.Invalid && bufferSize > 0)
            {
                return BitConverter.ToString(buffer, 0, (int)bufferSize).Replace("-", string.Empty);
            }

            throw new Exception("Failed to get Steam Auth token");
        }

        public async Task<bool> UpdateSessionToken()
        {
            var request = new RestRequest("api/v1/auth/provider/steam/login");
            request.AddQueryParameter("token", GetSTEAMSessionToken());
            request.AddJsonBody(new
            {
                clientData = new { consentId = "2" }
            });

            var response = await restClient.ExecutePostTaskAsync(request);
            if(response.StatusCode == HttpStatusCode.OK)
            {
                var session = response.Cookies;
                foreach(var cookie in response.Cookies)
                {
                    if(cookie.Name == "bhvrSession" &&
                        !string.IsNullOrEmpty(cookie.Value))
                    {
                        this.cookieJar.Add(new Cookie(cookie.Name, cookie.Value)
                        {
                            Domain = cookie.Domain
                        });

                        return true;
                    }
                }
            }

            SteamAPI.Shutdown();
            if (!SteamAPI.Init())
                throw new Exception("SteamAPI_Init failed!");

            return false;
        }

        public async Task<JObject> GetShrine(int retries = 0)
        {
            if (retries > 3)
                return null;

            var restRequest = new RestRequest("api/v1/extensions/shrine/getAvailable");
            restRequest.AddJsonBody(new {
                data = new {
                    version = "steam"
                }
            });

            var response = await restClient.ExecutePostTaskAsync(restRequest);
            if (response.StatusCode == HttpStatusCode.OK)
                return JObject.Parse(Encoding.ASCII.GetString(response.RawBytes));

            else
            {
                await UpdateSessionToken();
                return await GetShrine(retries + 1);
            }
        }

        public async Task<JObject> GetStoreOutfits()
        {
            var restRequest = new RestRequest("api/v1/extensions/store/getOutfits");
            restRequest.AddJsonBody(new
            {
                data = new { }
            });

            var response = await restClient.ExecutePostTaskAsync(restRequest);
            if (response.StatusCode == HttpStatusCode.OK)
                return JObject.Parse(Encoding.ASCII.GetString(response.RawBytes));

            return null;
        }
    }
}
