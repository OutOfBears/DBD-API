using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UnrealTools.Core;
using UnrealTools.Objects.Classes;
using TaggedItem = System.Collections.Generic.KeyValuePair<string, UnrealTools.Objects.Interfaces.IProperty>;
using Property = UnrealTools.Objects.Interfaces.IProperty;

namespace DBD_API.Modules.DbD.PakItems
{
    using TaggedItemsList = System.Collections.Generic.List<TaggedItem>;

    public class BaseItem : BaseInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; private set; }

        [JsonPropertyName("type")]
        public string Type { get; private set; }

        [JsonPropertyName("tags")]
        public string[] Tags { get; private set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; private set; }

        [JsonPropertyName("description")]
        public string Description { get; private set; }

        [JsonPropertyName("handPosition")]
        public string HandPosition { get; private set; }

        [JsonPropertyName("role")]
        public string Role { get; private set; }

        [JsonPropertyName("rarity")]
        public string Rarity { get; private set; }

        [JsonPropertyName("inventory")]
        public bool Inventory { get; private set; }

        [JsonPropertyName("chest")]
        public bool Chest { get; private set; }

        [JsonPropertyName("requiredKillerAbility")]
        public string RequiredKillerAbility { get; private set; }

        [JsonPropertyName("isInNonViolentBuild")]
        public bool IsInNonViolentBuild { get; private set; }

        [JsonPropertyName("isAvailableInAtlantaBuild")]
        public bool IsAvailableInAtlantaBuild { get; private set; }

        [JsonPropertyName("antiDLC")]
        public bool AntiDLC { get; private set; }

        [JsonPropertyName("bloodWeb")]
        public bool BloodWeb { get; private set; }

        [JsonPropertyName("iconPathList")]
        public string[] IconPathList { get; private set; }


        public BaseItem(TaggedItemsList itemList) : base(itemList)
        {
            ConvertItem<List<Property>>("Tags", x => Tags = x.Select(x => (x.Value as FName)?.Name.Value).ToArray());
            ConvertItem<FName>("HandPosition", x => HandPosition = x.Name);
            ConvertItem<FName>("Role", x => Role = x.Name);
            ConvertItem<FName>("Rarity", x => Rarity = x.Name);
            ConvertItem<bool>("Inventory", x => Inventory = x);
            ConvertItem<bool>("Chest", x => Chest = x);
            ConvertItem<FName>("RequiredKillerAbility", x => RequiredKillerAbility = x.Name);
            ConvertItem<bool>("IsInNonViolentBuild", x => IsInNonViolentBuild = x);
            ConvertItem<bool>("IsAvailableInAtlantaBuild", x => IsAvailableInAtlantaBuild = x);
            ConvertItem<bool>("AntiDLC", x => AntiDLC = x);
            ConvertItem<bool>("Bloodweb", x => BloodWeb = x);
            ConvertItem<FName>("ID", x => Id = x.Name);
            ConvertItem<FName>("Type", x => Type = x.Name);
            ConvertItem<TaggedObject>("UIData", x =>
            {
                var item = x.Vars.FirstOrDefault(x => x.Key == "DisplayName");
                if (!item.Equals(default) && (item.Value.Value is FText displayName))
                    DisplayName = displayName.ToString();

                item = x.Vars.FirstOrDefault(x => x.Key == "Description");
                if (!item.Equals(default) && (item.Value.Value is FText description))
                    Description = description.ToString();

                item = x.Vars.FirstOrDefault(x => x.Key == "IconFilePathList");
                if (!item.Equals(default) && (item.Value.Value is List<Property> paths))
                    IconPathList = paths.Select(y => y.Value.ToString()).ToArray();
            });
        }

    }
}
