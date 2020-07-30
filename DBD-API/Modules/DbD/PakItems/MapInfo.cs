using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UETools.Core;
using TaggedItem = System.Collections.Generic.KeyValuePair<string, UETools.Objects.Interfaces.IProperty>;
using Property = UETools.Objects.Interfaces.IProperty;

namespace DBD_API.Modules.DbD.PakItems
{
    using TaggedItemsList = System.Collections.Generic.List<TaggedItem>;
    public class MapInfo : BaseItem
    {
        [JsonPropertyName("mapId")]
        public string MapId { get; private set; }

        [JsonPropertyName("name")]
        public string Name { get; private set; }

        [JsonPropertyName("themeName")]
        public string ThemeName { get; private set; }

        [JsonPropertyName("description")]
        public string Description { get; private set; }

        [JsonPropertyName("hookMinDistance")]
        public double HookMinDistance { get; private set; }

        [JsonPropertyName("hookMinCount")]
        public double HookMinCount { get; private set; }

        [JsonPropertyName("hookMaxCount")]
        public double HookMaxCount { get; private set; }

        [JsonPropertyName("bookShelvesMinDistance")]
        public double BookShelvesMinDistance { get; private set; }

        [JsonPropertyName("bookShelvesMinCount")]
        public double BookShelvesMinCount { get; private set; }

        [JsonPropertyName("bookShelvesMaxCount")]
        public double BookShelvesMaxCount { get; private set; }

        [JsonPropertyName("livingWorldObjectsMinCount")]
        public double LivingWorldObjectsMinCount { get; private set; }

        [JsonPropertyName("livingWorldObjectsMaxCount")]
        public double LivingWorldObjectsMaxCount { get; private set; }

        [JsonPropertyName("thumbnailPath")]
        public string ThumbnailPath { get; private set; }

        [JsonPropertyName("sortingIndex")]
        public double SortingIndex { get; private set; }

        [JsonPropertyName("dlcIDString")]
        public string DlcIDString { get; private set; }

        [JsonPropertyName("isInNonViolentBuild")]
        public bool IsInNonViolentBuild { get; private set; }


        public MapInfo(TaggedItemsList itemList) : base(itemList)
        {
            ConvertItem<FName>("MapId", x => MapId = x.Name);
            ConvertItem<FText>("Name", x => Name = x.ToString());
            ConvertItem<FText>("ThemeName", x => ThemeName = x.ToString());
            ConvertItem<FText>("Description", x => Description = x.ToString());
            ConvertItem<FString>("ThumbnailPath", x => ThumbnailPath = x.Value);
            ConvertItem<FString>("DlcIDString", x => DlcIDString = x.ToString());
            ConvertItem<bool>("IsInNonViolentBuild", x => IsInNonViolentBuild = x);
            ConvertItem<int>("SortingIndex", x => SortingIndex = x);
            ConvertItem<int>("LivingWorldObjectsMaxCount", x => LivingWorldObjectsMaxCount = x);
            ConvertItem<int>("LivingWorldObjectsMinCount", x => LivingWorldObjectsMinCount = x);
            ConvertItem<int>("BookShelvesMaxCount", x => BookShelvesMaxCount = x);
            ConvertItem<int>("BookShelvesMinCount", x => BookShelvesMinCount = x);
            ConvertItem<int>("BookShelvesMinDistance", x => BookShelvesMinDistance = x);
            ConvertItem<int>("HookMaxCount", x => HookMaxCount = x);
            ConvertItem<int>("HookMinCount", x => HookMinCount = x);
            ConvertItem<int>("HookMinDistance", x => HookMinDistance = x);
        }
    }
}
