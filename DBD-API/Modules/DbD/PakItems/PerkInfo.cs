using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using UETools.Core;
using UETools.Objects.Classes;
using TaggedItem = System.Collections.Generic.KeyValuePair<string, UETools.Objects.Interfaces.IProperty>;
using Property = UETools.Objects.Interfaces.IProperty;

namespace DBD_API.Modules.DbD.PakItems
{
    using TaggedItemsList = System.Collections.Generic.List<TaggedItem>;

    public class PerkInfo : BaseItem
    {
        [JsonPropertyName("associatedPlayerIndex")]
        public int AssociatedPlayerIndex { get; private set; }

        [JsonPropertyName("mandatoryOnBloodWebLevel")]
        public int MandatoryOnBloodweblevel { get; private set; }

        [JsonPropertyName("teachableOnBloodWebLevel")]
        public int TeachableOnBloodweblevel { get; private set; }

        [JsonPropertyName("atlantaTeachableLevel")]
        public int AtlantaTeachableLevel { get; private set; }

        [JsonPropertyName("perkCategory")]
        public string[] PerkCategory { get; private set; }

        [JsonPropertyName("perkLevelRarity")]
        public string[] PerkLevelRarity { get; private set; }

        [JsonPropertyName("perkLevelTunables")]
        public string[][] PerkLevelTunables { get; private set; }

        [JsonPropertyName("perkDefaultDescription")]
        public string PerkDefaultDescription { get; private set; }

        [JsonPropertyName("perkLevel1Description")]
        public string PerkLevel1Description { get; private set; }

        [JsonPropertyName("perkLevel2Description")]
        public string PerkLevel2Description { get; private set; }

        [JsonPropertyName("perkLevel3Description")]
        public string PerkLevel3Description { get; private set; }

        [JsonPropertyName("perkUnlicensedDescriptionOverride")]
        public string PerkUnlicensedDescriptionOverride { get; private set; }

        [JsonPropertyName("iconPathList")]
        public string[] IconPathList { get; private set; }

        public PerkInfo(TaggedItemsList itemList) : base(itemList)
        {
            ConvertItem<int>("AssociatedPlayerIndex", x => AssociatedPlayerIndex = x);
            ConvertItem<int>("MandatoryOnBloodweblevel", x => MandatoryOnBloodweblevel = x);
            ConvertItem<int>("TeachableOnBloodweblevel", x => TeachableOnBloodweblevel = x);
            ConvertItem<int>("AtlantaTeachableLevel", x => AtlantaTeachableLevel = x);
            ConvertItem<List<Property>>("PerkCategory", x => PerkCategory = x.Select(x => (x.Value as FName)?.Name.Value) .ToArray());
            ConvertItem<List<Property>>("PerkLevelRarity", x => PerkLevelRarity = x.Select(x => (x.Value as FName)?.Name.Value).ToArray());
            ConvertItem<FText>("PerkDefaultDescription", x => PerkDefaultDescription = x.ToString());
            ConvertItem<FText>("PerkLevel1Description", x => PerkLevel1Description = x.ToString());
            ConvertItem<FText>("PerkLevel2Description", x => PerkLevel2Description = x.ToString());
            ConvertItem<FText>("PerkLevel3Description", x => PerkLevel3Description = x.ToString());
            ConvertItem<FText>("PerkUnlicensedDescriptionOverride", x => PerkUnlicensedDescriptionOverride = x.ToString());
            ConvertItem<TaggedObject>("UIData", x =>
            {
                var item = x.Vars.FirstOrDefault(x => x.Key == "IconFilePathList");
                if (!item.Equals(default) && (item.Value.Value is List<Property> paths))
                    IconPathList = paths.Select(y => y.Value.ToString()).ToArray();
            });
            ConvertItem<List<Property>>("PerkLevelTunables", x =>
            {
                PerkLevelTunables = x.Select(y =>
                {
                    var props = ((y.Value as TaggedObject)?.Vars.FirstOrDefault(z => z.Key == "Tunables").Value
                        .Value as List<Property>);
                    return props.Select(f => (f.Value as FString).Value).ToArray();
                }).ToArray();
            });
        }
    };
}
