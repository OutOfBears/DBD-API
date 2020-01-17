    using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DBD_API.Modules.DbD;
using DBD_API.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartFormat;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DBD_API.Controllers
{
    public class APIController : Controller
    {
        private DdbService _dbdService;
        private SteamService _steamService;

        // thanks for the help tricky
        private static readonly uint[] _rankSet = {
            // start, end, rank
            0, 2, 20,	// rank 20
            3, 5, 19,	// rank 19
            6, 9, 18,	// rank 18
            10, 13, 17,	// rank 17
            14, 17, 16,	// rank 16
            18, 21, 15,	// rank 15
            22, 25, 14,	// rank 14
            26, 29, 13,	// rank 13
            30, 34, 12,	// rank 12
            35, 39, 11,	// rank 11
            40, 44, 10,	// rank 10
            45, 49, 9,	// rank 9
            50, 54, 8,	// rank 8
            55, 59, 7,	// rank 7
            60, 64, 6,	// rank 6
            65, 69, 5,	// rank 5
            70, 74, 4,	// rank 4
            75, 79, 3,	// rank 3
            80, 84, 2,	// rank 2
            85, 89, 1,	// rank 1
        };

        // the purpose of this is to make the output more readable
        private static readonly Dictionary<string, string> _statsProxy = new Dictionary<string, string>()
        {
            { "survivorsMorid", "DBD_KilledCampers" },
            { "survivorsSacrified", "DBD_SacrificedCampers" },
            { "escapes", "DBD_Escape" },
            { "escapesKO", "DBD_EscapeKO" },
            { "escapesWithItem", "DBD_CamperNewItem" },
            { "escapesWithChestItem", "DBD_CamperEscapeWithItemFrom" },
            { "escapesWithNoDamageObsession", "DBD_EscapeNoBlood_Obsession" },
            { "chainsawHits", "DBD_ChainsawHit" },
            { "skillCheckSuccesses", "DBD_SkillCheckSuccess" },
            { "uncloakAttacks", "DBD_UncloakAttack" },
            { "hatchEscapes", "DBD_EscapeThroughHatch" },
            { "ultraRareOfferingsBurned", "DBD_BurnOffering_UltraRare" },
            { "hitsNearHook", "DBD_HitNearHook" },
            { "totalBpSpent", "DBD_BloodwebPoints" },
            { "allSurvivorsEscapedThruHatch", "DBD_AllEscapeThroughHatch" },
            { "trapsPickedUp", "DBD_TrapPickup" },
            { "timesHitNearHook", "DBD_HitNearHook" }
        };

        private static readonly Dictionary<string, string> _correctPerkNames = new Dictionary<string, string>()
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

        public APIController(
            DdbService dbdService,
            SteamService steamService
        )
        {
            _dbdService = dbdService;
            _steamService = steamService;
        }


        private static int PipsToRank(uint rank)
        {
            for(var i = 2; i < _rankSet.Length; i += 3)
                if (rank >= _rankSet[i - 2] && rank <= _rankSet[i - 1])
                    return (int)_rankSet[i];

            return -1;
        }

        private static string CorrectPerkName(string name)
        {
            if (_correctPerkNames.ContainsKey(name))
                return _correctPerkNames[name];

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

            return string.Join(" ", matches);
        }


        // API content
        public ActionResult Index()
        {
            var allowedPrefixes = string.Join(" ", DdbService.AllowedPrefixes);

            return Json(new
            {
                timestamp = DateTime.Now,
                message = "Welcome :)",
                contact = "Nexure#0001 (Discord), Nexurez (Twitch)",
                usage = new
                {
                    stats = "GET /api/stats/:steam_64: (Profile needs to be public)",
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

            }, new JsonSerializerOptions() { WriteIndented = true });
        }

        public async Task<ActionResult> Stats(ulong id = 0)
        {
            if (id == 0)
                return BadRequest("Invalid data, please pass steamid64");

            Dictionary<string, double> data = new Dictionary<string, double>();
            var result = await _steamService.GetUserStats(381210, id);
            if (result == null || result.Equals(default(Dictionary<string, double>)))
                return UnprocessableEntity("Unable to get player stats, maybe their profile isn't public");

            data["killerRank"] = result.ContainsKey("DBD_KillerSkulls") ?
                PipsToRank((uint)result["DBD_KillerSkulls"]) : 20;

            data["survivorRank"] = result.ContainsKey("DBD_CamperSkulls") ?
                PipsToRank((uint)result["DBD_CamperSkulls"]) : 20;

            foreach (var stat in _statsProxy)
                if (result.ContainsKey(stat.Value))
                    data[stat.Key] = result[stat.Value];
                else
                    data[stat.Key] = 0;
            
            return Json(data);
        }

        // TODO: implement me
        public async Task<ActionResult> Achievements(ulong id = 0)
        {
            return UnprocessableEntity("Not implemented yet");
            /*
            if (id == 0) return BadRequest("Invalid data, please pass steamid64");
            var result = await _steamService.GetUserStats(381210, id);
            return Ok();
            */
        }

        public async Task<ActionResult> ShrineOfSecrets(string branch = "live")
        {
            ShrineResponse shrine = null;

            try
            {
                shrine = await _dbdService.GetShrine(branch);
                if (shrine == null)
                    throw new InvalidOperationException();
            }
            catch (Exception)
            {
                return Content("Uh oh, we failed to retrieve the shrine from dbd servers :/");
            }
            
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
                return Json(shrine, new JsonSerializerOptions() { WriteIndented = true });
            }
        }

        public async Task<ActionResult> StoreOutfits(string branch = "live")
        {
            try
            {
                return Json(await _dbdService.GetStoreOutfits(branch), new JsonSerializerOptions() { WriteIndented = true });
            }
            catch
            {
                return Content("Uh oh, we failed to retrieve the shrine from dbd servers :/");
            }
        }

        public async Task<ActionResult> Config(string branch = "live") =>
             Content(await _dbdService.GetApiConfig(branch), "application/json");

        // CDN content
        public async Task<ActionResult> Catalog(string branch = "live")
            => Content(await _dbdService.GetCdnContent("/gameinfo/catalog.json", branch), "application/json");

        public async Task<ActionResult> News(string branch = "live")
            => Content(await _dbdService.GetCdnContent("/news/newsContent.json", branch), "application/json");

        public async Task<ActionResult> Featured(string branch = "live")
            => Content(await _dbdService.GetCdnContent("/banners/featuredPageContent.json", branch), "application/json");

        public async Task<ActionResult> Schedule(string branch = "live")
            => Content(await _dbdService.GetCdnContent("/schedule/contentSchedule.json", branch), "application/json");

        public async Task<ActionResult> BPEvents(string branch = "live")
            => Content(await _dbdService.GetCdnContent("/bonusPointEvents/bonusPointEventsContent.json", branch), "application/json");

        public async Task<ActionResult> SpecialEvents(string branch = "live")
            => Content(await _dbdService.GetCdnContent("/specialEvents/specialEventsContent.json", branch), "application/json");

        public async Task<ActionResult> ArchiveRewardData(string branch = "live")
            => Content(await _dbdService.GetCdnContent("/gameinfo/archiveRewardData/content.json", branch), "application/json");

        public async Task<ActionResult> Archive(string branch = "live", string tome = "Tome01")
        {
            tome = UrlEncoder.Create().Encode(tome);
            return Content(await _dbdService.GetCdnContent($"/gameinfo/archiveStories/v1/{tome}.json", branch), "application/json");
        }
    }
}
