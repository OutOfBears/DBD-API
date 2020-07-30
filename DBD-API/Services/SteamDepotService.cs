using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DBD_API.Modules.DbD;
using DBD_API.Modules.DbD.PakItems;
using Microsoft.Extensions.Hosting;

using DBD_API.Modules.Steam;
using DBD_API.Modules.Steam.ContentManagement;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.Extensions.Logging;
using SteamKit2;
using SteamKit2.Unified.Internal;
using UETools.Assets;
using UETools.Core.Enums;
using UETools.Core.Interfaces;
using UETools.Objects.Classes;
using UETools.Objects.Structures;
using UETools.Pak;

using CustomItemInfo = DBD_API.Modules.DbD.PakItems.CustomItemInfo;
using LocalizationTable = UETools.Core.Interfaces.IUnrealLocalizationProvider;

namespace DBD_API.Services
{
    public class SteamDepotService : IHostedService
    {
        private static string Deliminator = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/" : "\\";

        private static readonly Regex[] FileDownloadList = 
        {
            new Regex(@"(Paks\" + Deliminator +  @"pakchunk0\-WindowsNoEditor\.pak)$"),
            new Regex(@"(UI\" + Deliminator +  @".*?\.png)$"), 
        };

        private static readonly object[] AppDownloadList = 
        {
            new { Branch = "Public", AppId = 381210 },
        };


        private static Regex _tunableRegexFix = new Regex(@"(:?.\w+TunableDB$)");

        private CancellationTokenSource _cancellation;
        private ContentDownloader _contentDownloader;
        private CDNClientPool _cdnClientPool;

        private ILogger _logger;
        private uint _lastChangeNumber;

        private readonly DdbService _dbdService;
        private readonly SteamService _steamService;
        private readonly CallbackManager _callbackManager;
        
        public SteamDepotService(SteamService steamService, DdbService dbdService, ILogger<SteamDepotService> logger)
        {
            _dbdService = dbdService;
            _steamService = steamService;

            _callbackManager = new CallbackManager(_steamService.Client);
            _callbackManager.Subscribe<SteamApps.PICSChangesCallback>(OnPICSChanges);

            _lastChangeNumber = 0;
            _logger = logger;
        }

        private async Task<LocalizationTable> ReadLocalization(PakVFS pakReader)
        {
            var localizationTable = pakReader.AbsoluteIndex.FirstOrDefault(x => x.Key.Contains("/en/") && x.Key.EndsWith(".locres"));
            if (localizationTable.Equals(default)) 
                return null;

            await using var data = await localizationTable.Value.ReadAsync();
            data.Version = UE4Version.VER_UE4_AUTOMATIC_VERSION;
            LocResAsset.English.Deserialize(data);
            return LocResAsset.English;

        }
        
        private async Task LoadDBFromPak<T>(PakVFS pakReader, LocalizationTable localization, ConcurrentDictionary<string, T> itemDB, string dbName, string typeName = "",
            Func<TaggedObject, T, Task> onRowRead = null)
        {
            if (string.IsNullOrEmpty(typeName))
                typeName = dbName;

            foreach (var (_, db) in pakReader.AbsoluteIndex.Where(x => x.Key.EndsWith($"{dbName}.uasset")))
                await using (var data = await db.ReadAsync())
                {
                    data.Version = UE4Version.VER_UE4_AUTOMATIC_VERSION;
                    data.Localization = localization;
                    data.Read(out UAssetAsset asset);

                    var dataTable = asset.GetAssets()
                        .FirstOrDefault(x => x.FullName == $"DataTable {typeName}");

                    if (dataTable == null || !(dataTable.Object is UETools.Objects.Classes.DataTable items))
                        continue;

                    foreach (var (itemName, itemInfo) in items.Rows)
                    {
                        var item = (T) Activator.CreateInstance(typeof(T), itemInfo.Vars);
                        itemDB[itemName] = item;

                        if(onRowRead != null)
                            await onRowRead.Invoke(itemInfo, item);
                    }
                }
        }

        private async Task ReadMapInfo(PakVFS pakReader, LocalizationTable localization, string branch)
        {
            if (!_dbdService.MapInfos.TryGetValue(branch, out var mapInfos))
            {
                mapInfos = new ConcurrentDictionary<string, MapInfo>();
                _dbdService.MapInfos[branch] = mapInfos;
            }

            await LoadDBFromPak(pakReader, localization, mapInfos, "ProceduralMaps");
        }

        private async Task ReadPerkInfo(PakVFS pakReader, LocalizationTable localization, string branch)
        {
            if (!_dbdService.PerkInfos.TryGetValue(branch, out var perkInfos))
            {
                perkInfos = new ConcurrentDictionary<string, PerkInfo>();
                _dbdService.PerkInfos[branch] = perkInfos;
            }

            await LoadDBFromPak(pakReader, localization, perkInfos, "PerkDB");
        }

        private async Task ReadOfferingInfo(PakVFS pakReader, LocalizationTable localization, string branch)
        {
            if (!_dbdService.OfferingInfos.TryGetValue(branch, out var offeringInfos))
            {
                offeringInfos = new ConcurrentDictionary<string, OfferingInfo>();
                _dbdService.OfferingInfos[branch] = offeringInfos;
            }

            await LoadDBFromPak(pakReader, localization, offeringInfos, "OfferingDB");
        }

        private async Task ReadItemInfo(PakVFS pakReader, LocalizationTable localization, string branch)
        {
            if (!_dbdService.ItemInfos.TryGetValue(branch, out var itemInfos))
            {
                itemInfos = new ConcurrentDictionary<string, BaseItem>();
                _dbdService.ItemInfos[branch] = itemInfos;
            }

            await LoadDBFromPak(pakReader, localization, itemInfos, "ItemDB");
        }

        private async Task ReadItemAddonInfo(PakVFS pakReader, LocalizationTable localization, string branch)
        {
            if (!_dbdService.ItemAddonInfos.TryGetValue(branch, out var itemInfos))
            {
                itemInfos = new ConcurrentDictionary<string, ItemAddonInfo>();
                _dbdService.ItemAddonInfos[branch] = itemInfos;
            }

            await LoadDBFromPak(pakReader, localization, itemInfos, "ItemAddonDB");
        }

        private async Task ReadCustomItemInfo(PakVFS pakReader, LocalizationTable localization, string branch)
        {
            if (!_dbdService.CustomItemInfos.TryGetValue(branch, out var itemInfos))
            {
                itemInfos = new ConcurrentDictionary<string, CustomItemInfo>();
                _dbdService.CustomItemInfos[branch] = itemInfos;
            }

            await LoadDBFromPak(pakReader, localization, itemInfos, "ItemDB", "CustomizationItemDB");
        }

        private async Task ReadTunableInfo(PakVFS pakReader, LocalizationTable localization, string branch, string name, string tunable,
            ConcurrentDictionary<string, TunableInfo> tunableInfos)
        {
            tunable = _tunableRegexFix.Replace(tunable, ".uasset");
            tunable = tunable.Replace("/Game/", "DeadByDaylight/Content/");

            var pakPath = pakReader.AbsoluteIndex.FirstOrDefault(x => x.Key == tunable);
            if (pakPath.Equals(default) || pakPath.Value == null)
                return;


            await using (var data = await pakPath.Value.ReadAsync())
            {
                data.Version = UE4Version.VER_UE4_AUTOMATIC_VERSION;
                data.Localization = localization;
                data.Read(out UAssetAsset asset);

                var dataTable = asset.GetAssets()
                    .FirstOrDefault(x => x.FullName.StartsWith("DataTable"));

                if (dataTable == null || !(dataTable.Object is DataTable items))
                    return;

                tunableInfos[name] = new TunableInfo(items.Rows);
            }
        }

        private async Task ReadTunablesInfo(PakVFS pakReader, LocalizationTable localization, string branch)
        {
            if (!_dbdService.CharacterInfos.TryGetValue(branch, out var characterInfos))
                return;

            if (!_dbdService.TunableInfos.TryGetValue(branch, out var tunableInfos))
            {
                tunableInfos = new TunableContainer();
                _dbdService.TunableInfos[branch] = tunableInfos;
            }

            var miscTunables = pakReader.AbsoluteIndex.FirstOrDefault(x => x.Key.EndsWith("KillerTunableDB.uasset"));
            if (!miscTunables.Equals(default))
                await ReadTunableInfo(pakReader, localization, branch, "Killer", miscTunables.Key,
                    tunableInfos.BaseTunables);

            miscTunables = pakReader.AbsoluteIndex.FirstOrDefault(x => x.Key.EndsWith("SurvivorTunableDB.uasset"));
            if (!miscTunables.Equals(default))
                await ReadTunableInfo(pakReader, localization, branch, "Survivor", miscTunables.Key,
                    tunableInfos.BaseTunables);


            var regex = new Regex(@"^(\/Game\/Data\/Dlc\/\w+\/).*?$");
            var otherRegex = new Regex(@"^.*Dlc\/(.*?)\/TunableValuesDB\.uasset$");

            var matchedTunableValues = new List<string>();

            foreach (var characterTunable in characterInfos.Where(x => x.Value.TunablePath != "None"))
            {
                var character = characterTunable.Value;
                var match = regex.Match(character.TunablePath);
                await ReadTunableInfo(pakReader, localization, branch, character.IdName, character.TunablePath,
                    tunableInfos.KillerTunables);

                if (!match.Success)
                    continue;

                var matchPath = match.Groups[1].Value.Replace("/Game/", "DeadByDaylight/Content/")
                    + "TunableValuesDB.uasset";
                var tunableValueKv = pakReader.AbsoluteIndex.FirstOrDefault(x =>
                    x.Key == matchPath);
                if (tunableValueKv.Key == null)
                    continue;

                await ReadTunableInfo(pakReader, localization, branch, character.IdName, tunableValueKv.Key,
                    tunableInfos.KnownTunableValues);

                matchedTunableValues.Add(tunableValueKv.Key);
            }

            foreach (var tunableValues in pakReader.AbsoluteIndex.Where(x => x.Key.EndsWith("TunableValuesDB.uasset") &&
                                                                             !matchedTunableValues.Contains(x.Key)))
            {
                var path = tunableValues.Key;
                var name = otherRegex.Match(path);
                if (!name.Success)
                    continue;

                await ReadTunableInfo(pakReader, localization, branch, $"{name.Groups[1].Value}TunableValues", path,
                    tunableInfos.UnknownTunableValues);
            }
        }

        private async Task ReadCharacterInfo(PakVFS pakReader, LocalizationTable localization, string branch)
        {
            if (!_dbdService.CharacterInfos.TryGetValue(branch, out var characterInfos))
            {
                characterInfos = new ConcurrentDictionary<string, CharacterInfo>();
                _dbdService.CharacterInfos[branch] = characterInfos;
            }

            await LoadDBFromPak(pakReader, localization, characterInfos, "CharacterDescriptionDB");

            /*await LoadDBFromPak(pakReader, localization, characterInfos, "CharacterDescriptionDB", "",
                async (obj, parsedObj) =>
                {
                    var tunableKv = obj.Vars.FirstOrDefault(x => x.Key == "TunableDB");
                    if (tunableKv.Equals(default) || !(tunableKv.Value.Value is TaggedObject tunable))
                        return;

                    var ptrKv = tunable.Vars.FirstOrDefault(x => x.Key == "AssetPtr");
                    if (ptrKv.Equals(default) || !(ptrKv.Value.Value is var softObj))
                        return;

                    var path = softObj.ToString();
                    if (path != "None" && !string.IsNullOrEmpty(parsedObj.IdName))
                        await ReadTunablesInfo(pakReader, localization, branch, parsedObj.IdName, path);
                });
            */

        }

        // TODO: implement
        private async void OnPICSChanges(SteamApps.PICSChangesCallback callback)
        {
            if (callback.CurrentChangeNumber == _lastChangeNumber)
                return;

            foreach (var (key, _) in callback.AppChanges)
            {
                var appInList = AppDownloadList.Where(x => ((dynamic) x).AppId == key);
                foreach (var subApp in appInList)
                    await DownloadAppAsync(key, ((dynamic) subApp).Branch);
            }
        }


        private async Task DownloadAppAsync(uint appId, string branch)
        {
            try
            {
                await _contentDownloader.DownloadFilesAsync(appId, branch,
                    FileDownloadList.ToList(),
                    _cancellation.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to download app {1} '{0}': {2}", branch, appId, ex.Message);
                return;
            }

            try
            {
                var pakReader = await PakVFS.OpenAtAsync($"data/{appId}/{branch}/Paks");
                var localization = await ReadLocalization(pakReader);

                await ReadMapInfo(pakReader, localization, branch);
                await ReadPerkInfo(pakReader, localization, branch);
                await ReadOfferingInfo(pakReader, localization, branch);
                await ReadCustomItemInfo(pakReader, localization, branch);
                await ReadCharacterInfo(pakReader, localization, branch);
                await ReadItemAddonInfo(pakReader, localization, branch);
                await ReadItemInfo(pakReader, localization, branch);
                await ReadTunablesInfo(pakReader, localization, branch);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to read paks for app {1} '{0}': {2}", branch, appId, ex.Message);
                _logger.LogTrace("Stacktrace: {0}", ex.StackTrace);
            }
        }

        public async Task MainEvent()
        {
            if (!_steamService.LicenseCompletionSource.Task.IsCompleted)
                await _steamService.LicenseCompletionSource.Task;

            _logger.LogInformation("Got {0} licenses", _steamService.Licenses.Count);

            try
            {
                foreach (dynamic app in AppDownloadList)
                    await DownloadAppAsync((uint) app.AppId, (string) app.Branch);

            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to download app: {0}", ex);
            }

        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _cdnClientPool = new CDNClientPool(_steamService.Client, _cancellation);
            _contentDownloader = new ContentDownloader(_steamService.Client, _cdnClientPool, _steamService.Licenses, _logger);

            var task = Task.Factory.StartNew(MainEvent, _cancellation.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            return task.IsCompleted ? task : Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellation?.Cancel();
            return Task.CompletedTask;
        }
    }
}
