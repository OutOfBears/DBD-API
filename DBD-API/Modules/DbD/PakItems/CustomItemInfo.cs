using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UnrealTools.Core;
using UnrealTools.Objects.Classes;
using Property = UnrealTools.Objects.Interfaces.IProperty;
using TaggedItem = System.Collections.Generic.KeyValuePair<string, UnrealTools.Objects.Interfaces.IProperty>;

namespace DBD_API.Modules.DbD.PakItems
{
    using TaggedItemsList = System.Collections.Generic.List<TaggedItem>;

    public class CustomItemInfo : BaseInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; private set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; private set; }

        [JsonPropertyName("description")]
        public string Description { get; private set; }

        [JsonPropertyName("category")]
        public string Category { get; private set; }

        [JsonPropertyName("rarity")]
        public string Rarity { get; private set; }

        [JsonPropertyName("associatedRole")]
        public string AssociatedRole { get; private set; }

        [JsonPropertyName("collectionName")]
        public string CollectionName { get; private set; }

        [JsonPropertyName("collectionDescription")]
        public string CollectionDescription { get; private set; }

        [JsonPropertyName("platformExclusiveFlag")]
        public uint PlatformExclusiveFlag { get; private set; }

        [JsonPropertyName("associatedCharacter")]
        public int AssociatedCharacter { get; private set; }

        [JsonPropertyName("prestigeUnlockLevel")]
        public int PrestigeUnlockLevel { get; private set; }

        [JsonPropertyName("prestigeUnlockDate")]
        public string PrestigeUnlockDate { get; private set; }

        [JsonPropertyName("itemIsInStore")]
        public bool ItemIsInStore { get; private set; }

        [JsonPropertyName("isNonVioletBuild")]
        public bool IsNonVioletBuild { get; private set; }

        [JsonPropertyName("isAvailableInAtlantaBuild")]
        public bool IsAvailableInAtlantaBuild { get; private set; }

        public CustomItemInfo(TaggedItemsList itemList) : base(itemList)
        {
            ConvertItem<FName>("ID", x => Id = x.Name);
            ConvertItem<FName>("Category", x => Category = x.Name);
            ConvertItem<FName>("Rarity", x => Rarity = x.Name);
            ConvertItem<FName>("AssociatedRole", x => AssociatedRole = x.Name);
            ConvertItem<FText>("CollectionName", x => CollectionName = x.ToString());
            ConvertItem<FText>("CollectionDescription", x => CollectionDescription = x.ToString());
            ConvertItem<uint>("PlatformExclusiveFlag", x => PlatformExclusiveFlag = x);
            ConvertItem<int>("AssociatedCharacter", x => AssociatedCharacter = x);
            ConvertItem<int>("PrestigeUnlockLevel", x => PrestigeUnlockLevel = x);
            ConvertItem<FString>("PrestigeUnlockDate", x => PrestigeUnlockDate = x.Value);
            ConvertItem<bool>("ItemIsInStore", x => ItemIsInStore = x);
            ConvertItem<bool>("IsNonVioletBuild", x => IsNonVioletBuild = x);
            ConvertItem<bool>("IsAvailableInAtlantaBuild", x => IsAvailableInAtlantaBuild = x);
            ConvertItem<TaggedObject>("UIData", x =>
            {
                var item = x.Vars.FirstOrDefault(x => x.Key == "DisplayName");
                if (!item.Equals(default) && (item.Value.Value is FText displayName))
                    DisplayName = displayName.ToString();

                item = x.Vars.FirstOrDefault(x => x.Key == "Description");
                if (!item.Equals(default) && (item.Value.Value is FText description))
                    Description = description.ToString();

            });

        }
    }
}
