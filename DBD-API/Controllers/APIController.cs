using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DBD_API.Modules.DbD;
using DBD_API.Modules.DbD.JsonResponse;
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

        private static readonly uint[] _rankSet = {
            85, 80, 75, 70, 65, 60, 55, 50,
            45, 40, 35, 30, 26, 22, 18, 14,
            10,  6,  3,  0
        };

        // the purpose of this is to make the output more readable
        private static readonly Dictionary<string, string> _statsProxy = new Dictionary<string, string>()
        {
            { "survivorsMorid", "DBD_KilledCampers" },
            { "survivorsSacrificed", "DBD_SacrificedCampers" },
            { "escapes", "DBD_Escape" },
            { "escapesKO", "DBD_EscapeKO" },
            { "escapesWithItem", "DBD_CamperNewItem" },
            { "escapesWithItemFromDeadPlayer", "DBD_CamperEscapeWithItemFrom" },
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
            { "killerPerfectMatches", "DBD_SlasherMaxScoreByCategory" },
            { "survivorPerfectMatches", "DBD_CamperMaxScoreByCategory" },
            { "survivorsHitWhileCarrying", "DBD_Chapter10_Slasher_Stat1" },
            { "survivorsCaughtDoingGen", "DBD_Chapter12_Slasher_Stat1" },
            { "obsessionsSacrificed", "DBD_Chapter12_Slasher_Stat1" },
            { "survivorsKilledInEndGame", "DBD_DLC8_Slasher_Stat2" },
            { "survivorsKilledBeforeEndGame", "DBD_Chapter11_Slasher_Stat1" },
            { "hatchesClosed", "DBD_Chapter13_Slasher_Stat1" },
            { "generatorsCompleted", "DBD_GeneratorPct_float" },
            { "healingStatesCompleted",  "DBD_HealPct_float" },
            { "survivorUnhookOrHeals", "DBD_UnhookOrHeal" },
            { "usedUltraRareAndEscaped", "DBD_CamperKeepUltraRare" },
            { "chestsOpened", "DBD_DLC7_Camper_Stat1" },
            { "hexTotemsCleansed", "DBD_DLC3_Camper_Stat1" },
            { "killerGraspsEscaped", "DBD_Chapter12_Camper_Stat1" },
            { "selfSurvivorHookEscape", "DBD_Chapter9_Camper_Stat1" },
            { "hooksSabotaged", "DBD_Chapter10_Camper_Stat1"},
            { "myersTierUps", "DBD_SlasherTierIncrement " }, 
            { "survivorsPutToSleep", "DBD_DLC7_Slasher_Stat1" },
            { "mysteryBoxesOpened", "DBD_Event1_Stat3" },
            { "survivorsPutIntoDeepWound", "DBD_Chapter10_Slasher_Stat2" },
            { "healingStatesCompletedWhileInjured", "DBD_Chapter11_Camper_Stat1_float" },
            { "survivorsMarkedWithGhostFace", "DBD_Chapter12_Slasher_Stat2" },
            { "survivorsHookedWhileTeamInjured", "DBD_Chapter14_Slasher_Stat1" },
            { "wentIntoMatchWithFullSurvivorLayout", "DBD_CamperFullLoadout" },
            { "wentIntoMatchWithFullKillerLayout", "DBD_SlasherFullLoadout" },
            { "gensKickedCompletelyRepaired", "DBD_Camper8_Stat1" },
            { "survivorVaultsWhileChased", "DBD_Camper8_Stat2" },
            { "exitGatesOpened", "DBD_DLC7_Camper_Stat2" },
            { "bearTrapsPlacedAsPig", "DBD_DLC8_Slasher_Stat1" },
            { "timesThreeSurvivorsHookedInBasement", "DBD_Event1_Stat1" },
            { "gensKickedAfterSurvivorHook", "DBD_DLC9_Slasher_Stat1" },
            { "survivorsHitWhoDroppedPalletInChase", "DBD_Chapter9_Slasher_Stat1" },
            { "hagTrapsTriggered", "DBD_DLC3_Slasher_Stat1" },
            { "survivorsShockedWithDoc", "DBD_DLC4_Slasher_Stat1" },
            { "hatchetThrowsWithHuntress", "DBD_DLC5_Slasher_Stat1" },
            { "bubbaChainsawHits", "DBD_DLC6_Slasher_Stat1" }, 
            { "nurseBlinkAttacks", "DBD_SlasherChainAttack" },
            { "hitsWithHatchets24MOrMore", "DBD_DLC5_Slasher_Stat2"},
            { "survivorsPlagInfectedDowned", "DBD_Chapter11_Slasher_Stat2" },
            { "survivorsHookedInBasement", "DBD_DLC6_Slasher_Stat2" },
            { "survivorsDownedAfterPhaseWalk", "DBD_Chapter9_Slasher_Stat2" },
            { "survivorsDownedWithShred", "DBD_Chapter13_Slasher_Stat2" },
            { "survivorsDownedWhileInDemonFury", "DBD_Chapter14_Slasher_Stat2" },
            { "survivorsDownedWhilePoisonedClown", "DBD_DLC9_Slasher_Stat2" },
            { "secondFloorGenAndEscapedLampkinLane", "DBD_FixSecondFloorGenerator_MapSub_Street" },
            { "secondFloorGenAndEscapedMothersDwelling", "DBD_FixSecondFloorGenerator_MapBrl_MaHouse" },
            { "secondFloorGenAndEscapedFatherCampbells Chapel", "DBD_FixSecondFloorGenerator_MapAsy_Chapel" },
            { "secondFloorGenAndEscapedDisturbedWard", "DBD_FixSecondFloorGenerator_MapAsy_Asylum" },
            { "secondFloorGenAndEscapedYamaokaResidence", "DBD_FixSecondFloorGenerator_MapHti_Manor" },
            { "secondFloorGenAndEscapedTempleOfPurgation", "DBD_FixSecondFloorGenerator_MapBrl_Temple" },
            { "secondFloorGenAndEscapedHawkinsLab", "DBD_FixSecondFloorGenerator_MapQat_Lab" },
            { "secondFloorGenAndEscapedSanctumOfWrath", "DBD_FixSecondFloorGenerator_MapHti_Shrine" },
            { "secondFloorGenAndEscapedMountOrmond", "DBD_FixSecondFloorGenerator_MapKny_Cottage" },
            { "secondFloorGenAndEscapedThePaleRose", "DBD_FixSecondFloorGenerator_MapSwp_PaleRose" },
            { "secondFloorGenAndEscapedTheGame", "DBD_FixSecondFloorGenerator_MapFin_Hideout" },
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
            for (var i = 0; i < _rankSet.Length; i++)
                if (rank >= _rankSet[i])
                    return i + 1;

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

        private static string BranchToDepotBranch(string branch)
        {
            switch (branch)
            {
                case "live":
                    return "Public";

                default:
                    return "";
            }
        }

        private JsonResult PrettyJson(object x)
        {
            return Json(x, new JsonSerializerOptions() { IgnoreNullValues = true });
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
                    maps = "GET /api/maps(?branch=live)",
                    perks = "GET /api/perks(?branch=live)",
                    offerings = "GET /api/offerings(?branch=live)",
                    characters = "GET /api/characters(?branch=live)",
                    tunables = "GET /api/tunables(?branch=live&killer=)",
                    emblemtunnables = "GET /api/emblemtunables(?branch=live)",
                    gameconfigs = "GET /api/gameconfigs(?branch=live)",
                    ranksthresholds = "GET /api/ranksthresholds(?branch=live)",
                    customizationitems = "GET /api/customizationitems(?branch=live)",
                    itemaddons = "GET /api/itemaddons(?branch=live)",
                    items = "GET /api/items(?branch=live)",
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
                ps = $"only the whitelisted branches are allowed ({allowedPrefixes}). Also pls dont spam any endpoints, I dont wanna have to add rate limitng..."

            }, new JsonSerializerOptions() { WriteIndented = true });
        }

        public ActionResult Maps(string branch = "live")
        {
            var depotBranch = BranchToDepotBranch(branch);
            if (string.IsNullOrEmpty(depotBranch))
                return BadRequest($"Branch '{branch}' not supported");

            if (_dbdService.MapInfos.Count < 1 || _dbdService.MapInfos[depotBranch].Count < 1)
                return Unauthorized($"Maps for branch '{branch}' not yet loaded");

            return Json(_dbdService.MapInfos[depotBranch], new JsonSerializerOptions() { IgnoreNullValues = true });
        }

        public ActionResult Items(string branch = "live", string id = "")
        {
            var depotBranch = BranchToDepotBranch(branch);
            if (string.IsNullOrEmpty(depotBranch))
                return BadRequest($"Branch '{branch}' not supported");

            if (_dbdService.ItemInfos.Count < 1 || _dbdService.ItemInfos[depotBranch].Count < 1)
                return Unauthorized($"Items for branch '{branch}' not yet loaded");


            return string.IsNullOrEmpty(id) ?
                PrettyJson(_dbdService.ItemInfos[depotBranch]) :
                PrettyJson(_dbdService.ItemInfos[depotBranch][id]);
        }

        public ActionResult ItemAddons(string branch = "live")
        {
            var depotBranch = BranchToDepotBranch(branch);
            if (string.IsNullOrEmpty(depotBranch))
                return BadRequest($"Branch '{branch}' not supported");

            if (_dbdService.ItemAddonInfos.Count < 1 || _dbdService.ItemAddonInfos[depotBranch].Count < 1)
                return Unauthorized($"Items for branch '{branch}' not yet loaded");

            return Json(_dbdService.ItemAddonInfos[depotBranch], new JsonSerializerOptions() { IgnoreNullValues = true });
        }

        public ActionResult Characters(string branch = "live", string type = "all", int id = -1)
        {
            var depotBranch = BranchToDepotBranch(branch);
            if (string.IsNullOrEmpty(depotBranch))
                return BadRequest($"Branch '{branch}' not supported");

            if (_dbdService.CharacterInfos.Count < 1 || _dbdService.CharacterInfos[depotBranch].Count < 1)
                return Unauthorized($"Characters for branch '{branch}' not yet loaded");

            if (id > -1)
                return Json(_dbdService.CharacterInfos[depotBranch].SearchOne(x => x.CharacterIndex == id));

            var infos = _dbdService.CharacterInfos[depotBranch];
            switch (type)
            {
                case "survivor":
                    return Json(infos.Where(x => x.Value.Role == "EPlayerRole::VE_Camper")
                        .ToDictionary(x => x.Key, x => x.Value));

                case "killer":
                    return Json(infos.Where(x => x.Value.Role == "EPlayerRole::VE_Slasher")
                        .ToDictionary(x => x.Key, x => x.Value));

                default:
                    break;
            }

            return Json(infos);

        }

        public ActionResult Tunables(string branch = "live")
        {
            var depotBranch = BranchToDepotBranch(branch);
            if (string.IsNullOrEmpty(depotBranch))
                return BadRequest($"Branch '{branch}' not supported");

            if (_dbdService.TunableInfos.Count < 1 || !_dbdService.TunableInfos.TryGetValue(depotBranch, out var tunableContainer))
                return Unauthorized($"Tunables for branch '{branch}' not yet loaded");

            return Json(tunableContainer);
        }

        public ActionResult Perks(string branch = "live", int id = -1)
        {
            var depotBranch = BranchToDepotBranch(branch);
            if (string.IsNullOrEmpty(depotBranch))
                return BadRequest($"Branch '{branch}' not supported");

            if (_dbdService.PerkInfos.Count < 1 || _dbdService.PerkInfos[depotBranch].Count < 1)
                return Unauthorized($"Perks for branch '{branch}' not yet loaded");


            return id > -1 ? Json(_dbdService.PerkInfos[depotBranch].SearchMany(x => x.AssociatedPlayerIndex == id)) 
                : Json(_dbdService.PerkInfos[depotBranch]);
        }

        public ActionResult Offerings(string branch = "live")
        {
            var depotBranch = BranchToDepotBranch(branch);
            if (string.IsNullOrEmpty(depotBranch))
                return BadRequest($"Branch '{branch}' not supported");

            if (_dbdService.OfferingInfos.Count < 1 || _dbdService.OfferingInfos[depotBranch].Count < 1)
                return Unauthorized($"Offerings for branch '{branch}' not yet loaded");
            
            return Json(_dbdService.OfferingInfos[depotBranch]);
        }

        public ActionResult CustomizationItems(string branch = "live")
        {
            var depotBranch = BranchToDepotBranch(branch);
            if (string.IsNullOrEmpty(depotBranch))
                return BadRequest($"Branch '{branch}' not supported");

            if (_dbdService.CustomItemInfos.Count < 1 || _dbdService.CustomItemInfos[depotBranch].Count < 1)
                return Unauthorized($"Customizations items for branch '{branch}' not yet loaded");

            return Json(_dbdService.CustomItemInfos[depotBranch]);
        }

        public async Task<ActionResult> Stats(ulong id = 0)
        {
            if (id == 0)
                return BadRequest("Invalid data, please pass steamid64");

            Dictionary<string, object> data = new Dictionary<string, object>();
            var result = await _steamService.GetUserStats(381210, id);
            if (result == null || result.Equals(default(Dictionary<string, object>)))
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
            ShrineResponse shrine;

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

            var useRealFix = _dbdService.PerkInfos.TryGetValue(BranchToDepotBranch(branch),
                out var perkInfos);

            foreach (var item in shrine.Items)
                if (useRealFix && perkInfos.TryGetValue(item.Id, out var perk))
                    item.Name = perk.DisplayName;
                else
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
                return Json(shrine, ShrineConverter.Settings);
            }
        }

        public async Task<ActionResult> StoreOutfits(string branch = "live")
        {
            try
            {
                var outfits = await _dbdService.GetStoreOutfits(branch);
                if (outfits == null)
                    throw new Exception("Invalid response from dbd");

                return Json(outfits, StoreConverter.Settings);
            }
            catch
            {
                return Content("Uh oh, we failed to retrieve the sto from dbd servers :/");
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

        public async Task<ActionResult> EmblemTunables(string branch = "live")
            => Content(await _dbdService.GetCdnContentFormat("/content/{0}/emblemTunable.json", branch), "application/json");

        public async Task<ActionResult> RanksThresholds(string branch = "live")
            => Content(await _dbdService.GetCdnContentFormat("/content/{0}/ranksThresholds.json", branch), "application/json");

        public async Task<ActionResult> GameConfigs(string branch = "live")
            => Content(await _dbdService.GetCdnContentFormat("/content/{0}/GameConfigs.json", branch), "application/json");

        public async Task<ActionResult> Archive(string branch = "live", string tome = "Tome01")
        {
            tome = UrlEncoder.Create().Encode(tome);
            return Content(await _dbdService.GetCdnContent($"/gameinfo/archiveStories/v1/{tome}.json", branch), "application/json");
        }
    }
}
