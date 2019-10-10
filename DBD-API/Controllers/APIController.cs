using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DBD_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DBD_API.Controllers
{
    public class APIController : Controller
    {
        private DdbService _dbdService;

        public APIController(
            DdbService dbdService
        )
        {
            _dbdService = dbdService;
        }

        public ActionResult Index()
        {
            var allowedPrefixes = DdbService.AllowedPrefixes.Join(", ");

            return Json(new
            {
                timestamp = DateTime.Now,
                message = "Welcome :)",
                contact = "Nexure#0001 (Discord), Nexurez (Twitch)",
                usage = new
                {
                    shrine = "GET /api/shrineofsecrets(?pretty=true)",
                    store = "GET /api/storeoutfits",
                    config = "GET /api/config",
                    catalog = "GET /api/catalog(?branch=live)",
                    news = "GET /api/news(?branch=live)",
                    featured = "GET /api/featured(?branch=live)",
                    schedule = "GET /api/schedule(?branch=live)",
                    bloodpointEvents = "GET /api/bpevents(?branch=live)",
                    specialevents = "GET /api/specialevents(?branch=live)",
                    stories = "GET /api/stories(?branch=ptb)"
                },
                ps = $"only the whitelisted branches are allowed ({allowedPrefixes})"

            }, new JsonSerializerSettings() { Formatting = Formatting.Indented });
        }
        

        private static string CorrectPerkName(string name)
        {
            var weirdNames = new Dictionary<string, string>
            {
                { "InTheDark", "Knock Out" },
                { "SelfSufficient", "Unbreakable" },
                { "FranklinsLoss", "Franklins Demise" },
                { "BBQAndChilli", "Barbecue & Chilli" },
                { "Madgrit", "Mad Grit" },
                { "GeneratorOvercharge", "Overcharge" },
            };

            if (weirdNames.ContainsKey(name))
                return weirdNames[name];

            var pattern = new Regex("([A-Za-z]([a-z0-9:]+))");
            var matches = pattern.Matches(name)
                .Select(match => match.Value)
                .ToList();

            if (matches.Count > 0)
            {
                if (matches[0] == "Hex")
                    matches[0] = "Hex:";
            }
            else
                matches.Add(name);

            return matches.Join(" ");
        }
        

        // API content
        public async Task<ActionResult> ShrineOfSecrets()
        {
            JObject shrine = null;
            try
            {
                shrine = await _dbdService.GetShrine();
            }
            catch (Exception)
            {
                return Content("Uh oh, we failed to retrieve the shrine from dbd servers :/");
            }

            if (!string.IsNullOrEmpty(Request.Query["pretty"]))
            {
                if(shrine != null)
                {
                    try
                    {
                        var output = new StringBuilder();

                        var items = shrine["items"].ToArray();
                        var start = (DateTime)shrine["startDate"];
                        var end = (DateTime)shrine["endDate"];

                        foreach(var item in items)
                        {
                            var name = CorrectPerkName((string)item["id"]);
                            var cost = item["cost"].ToArray();
                            if (!cost.Any())
                                continue;

                            var price = (int)cost[0]["price"];

                            output.Append($"{name} : {price}");

                            if (item != items.Last())
                                output.Append(", ");
                        }

                        output.Append(" | ");

                        var changesIn = end - DateTime.Now;
                        output.Append(
                            $"Shrine changes in {changesIn.Days} days, {changesIn.Hours} hours, and {changesIn.Minutes} mins");

                        return Content(output.ToString());
                    }
                    catch
                    {
                        return Content("Uhhhh, we failed to parse the data retrieved, contact me to fix");
                    }
                }
                else
                {
                    return Content("Uh oh, we failed to retrieve the shrine from dbd servers :/");
                }
            }
            else
            {
                return Json(shrine, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }
        }

        public async Task<ActionResult> StoreOutfits()
        {
            try
            {
                return Json(await _dbdService.GetStoreOutfits(), new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }
            catch
            {
                return Content("Uh oh, we failed to retrieve the shrine from dbd servers :/");
            }
        }

        public async Task<ActionResult> Config() =>
             Content(await _dbdService.GetApiConfig());

        // CDN content

        public async Task<ActionResult> Catalog(string branch = "live")
            => Conflict(await _dbdService.GetCdnContent("/gameinfo/catalog.json", branch));

        public async Task<ActionResult> News(string branch = "live")
            => Content(await _dbdService.GetCdnContent("/news/newsContent.json", branch));

        public async Task<ActionResult> Featured(string branch = "live")
            => Content(await _dbdService.GetCdnContent("/banners/featuredPageContent.json", branch));

        public async Task<ActionResult> Schedule(string branch = "live")
            => Content(await _dbdService.GetCdnContent("/schedule/contentSchedule.json", branch));

        public async Task<ActionResult> BPEvents(string branch = "live")
            => Content(await _dbdService.GetCdnContent("/bonusPointEvents/bonusPointEventsContent.json", branch));

        public async Task<ActionResult> SpecialEvents(string branch = "live")
            => Content(await _dbdService.GetCdnContent("/specialEvents/specialEventsContent.json", branch));

        public async Task<ActionResult> Stories(string branch = "ptb")
            => Content(await _dbdService.GetCdnContent("/gameinfo/archiveStories/v1/Tome01.json", branch));
    }
}
