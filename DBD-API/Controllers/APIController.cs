using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DBD_API.Modules.DbD;
using DBD_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartFormat;

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
                    shrine = "GET /api/shrineofsecrets(?pretty=true&branch=live)",
                    store = "GET /api/storeoutfits(?branch=live)",
                    config = "GET /api/config(?branch=live)",
                    catalog = "GET /api/catalog(?branch=live)",
                    news = "GET /api/news(?branch=live)",
                    featured = "GET /api/featured(?branch=live)",
                    schedule = "GET /api/schedule(?branch=live)",
                    bloodpointEvents = "GET /api/bpevents(?branch=live)",
                    specialevents = "GET /api/specialevents(?branch=live)",
                    archive = "GET /api/archive(?branch=ptb&tome=Tome01)",
                    achiveRewardData = "GET /api/archiverewarddata(?branch=live)"
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
                { "FranklinsLoss", "Franklin's Demise" },
                { "BBQAndChilli", "Barbecue & Chilli" },
                { "Madgrit", "Mad Grit" },
                { "GeneratorOvercharge", "Overcharge" },
                { "TheMettleOfMan", "Mettle Of Man" },
                { "pop_goes_the_weasel", "Pop Goes The Weasel" },
                { "MonitorAndAbuse", "Monitor & Abuse" },
                { "No_One_Escapes_Death", "Hex: No One Escapes Death" },
                { "HangmansTrick", "Hangman's Trick" },
                { "ImAllEars", "I'm All Ears" },
                { "NurseCalling", "A Nurse's Calling" },
                { "WellMakeIt", "We'll Make It" },
                { "WakeUp", "Wake Up!" },
                { "Plunderers_Instinct", "Plunderer's Instinct" }
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
        public async Task<ActionResult> ShrineOfSecrets(string branch = "live")
        {
            ShrineResponse shrine = null;

            try
            {
                shrine = await _dbdService.GetShrine(branch);
            }
            catch (Exception)
            {
                return Content("Uh oh, we failed to retrieve the shrine from dbd servers :/");
            }

            if (shrine == null)
                return Content("Uh oh, we failed to retrieve the shrine from dbd servers :/");

            foreach (var item in shrine.Items)
                item.Name = CorrectPerkName(item.Id);

            var format = (string) Request.Query["format"];
            if (!string.IsNullOrEmpty(Request.Query["pretty"]))
            {
                if (!string.IsNullOrEmpty(format))
                {
                    try
                    {
                        return Content(Smart.Format(format, shrine));
                    }
                    catch (Exception e)
                    {
                        return Json(new
                        {
                            success = false,
                            type = "Format Exception",
                            reason = e.Message
                        });
                    }
                }
                else
                    try
                    {
                        var output = new StringBuilder();
                        
                        foreach(var item in shrine.Items)
                        {
                            if (!item.Cost.Any())
                                continue;
                            
                            output.Append($"{item.Name} : {item.Cost[0].Price}");

                            if (item != shrine.Items.Last())
                                output.Append(", ");
                        }

                        output.Append(" | ");

                        var changesIn = shrine.EndDate - DateTime.Now;
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
                return Json(shrine, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }
        }

        public async Task<ActionResult> StoreOutfits(string branch = "live")
        {
            try
            {
                return Json(await _dbdService.GetStoreOutfits(branch), new JsonSerializerSettings() { Formatting = Formatting.Indented });
            }
            catch
            {
                return Content("Uh oh, we failed to retrieve the shrine from dbd servers :/");
            }
        }

        public async Task<ActionResult> Config(string branch = "live") =>
             Content(await _dbdService.GetApiConfig(branch));

        // CDN content
        public async Task<ActionResult> Catalog(string branch = "live")
            => Content(await _dbdService.GetCdnContent("/gameinfo/catalog.json", branch));

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

        public async Task<ActionResult> ArchiveRewardData(string branch = "live")
            => Content(await _dbdService.GetCdnContent("/gameinfo/archiveRewardData/content.json", branch));

        public async Task<ActionResult> Archive(string branch = "live", string tome = "Tome01")
        {
            tome = UrlEncoder.Create().Encode(tome);
            return Content(await _dbdService.GetCdnContent($"/gameinfo/archiveStories/v1/{tome}.json", branch));
        }
    }
}
