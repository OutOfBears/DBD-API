using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SteamKit2.Unified.Internal;
using UnrealTools.Core;
using UnrealTools.Objects.Classes;
using TaggedItem = System.Collections.Generic.KeyValuePair<string, UnrealTools.Objects.Interfaces.IProperty>;
using Property = UnrealTools.Objects.Interfaces.IProperty;

namespace DBD_API.Modules.DbD.PakItems
{
    using TaggedItemsList = System.Collections.Generic.List<TaggedItem>;

    public struct SlideDescription
    {
        [JsonPropertyName("overview")]
        public string Overview { get; set; }

        [JsonPropertyName("playStyle")]
        public string Playstyle { get; set; }
    }

    public class CharacterInfo : BaseInfo
    {
        [JsonPropertyName("characterIndex")]
        public int CharacterIndex { get; private set; }

        [JsonPropertyName("role")]
        public string Role { get; private set; }

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; private set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; private set; }

        [JsonPropertyName("backStory")]
        public string Backstory { get; private set; }

        [JsonPropertyName("biography")]
        public string Biography { get; private set; }

        [JsonPropertyName("requiredDlcIdString")]
        public string RequiredDlcIdString { get; private set; }

        [JsonPropertyName("idName")]
        public string IdName { get; private set; }

        [JsonPropertyName("defaultItem")]
        public string DefaultItem { get; private set; }

        [JsonPropertyName("isAvailableInNonViolentBuild")]
        public bool IsAvailableInNonViolentBuild { get; private set; }

        [JsonPropertyName("isAvailableInAtlantaBuild")]
        public bool IsAvailableInAtlantaBuild { get; private set; }

        [JsonPropertyName("platformExclusiveFlag")]
        public uint PlatformExclusiveFlag { get; private set; }

        [JsonPropertyName("killerAbilities")]
        public string[] KillerAbilities { get; private set; }

        [JsonPropertyName("gender")]
        public string Gender { get; private set; }

        [JsonPropertyName("killerHeight")]
        public string KillerHeight { get; private set; }

        [JsonPropertyName("iconPath")]
        public string IconPath { get; private set; }

        [JsonPropertyName("backgroundPath")]
        public string BackgroundPath { get; private set; }

        [JsonPropertyName("slideShowDescriptions")]
        public SlideDescription SlideShowDescriptions { get; private set; }
        
        [JsonIgnore]
        public string TunablePath { get; private set; }


        public CharacterInfo(TaggedItemsList itemList) : base(itemList)
        {
            ConvertItem<int>("CharacterIndex", x => CharacterIndex = x);
            ConvertItem<FName>("Role", x => Role = x.Name);
            ConvertItem<FName>("Difficulty", x => Difficulty= x.Name);
            ConvertItem<FText>("DisplayName", x => DisplayName = x.ToString());
            ConvertItem<FText>("BackStory", x => Backstory = x.ToString());
            ConvertItem<FText>("Biography", x => Biography = x.ToString());
            ConvertItem<FString>("RequiredDlcIDString", x => RequiredDlcIdString = x.Value);
            ConvertItem<FName>("IdName", x => IdName = x.Name);
            ConvertItem<bool>("IsAvailableInNonViolentBuild", x => IsAvailableInNonViolentBuild = x);
            ConvertItem<bool>("IsAvailableInAtlantaBuild", x => IsAvailableInAtlantaBuild = x);
            ConvertItem<uint>("PlatformExclusiveFlag", x => PlatformExclusiveFlag = x);
            ConvertItem<FName>("DefaultItem", x => DefaultItem = x.Name);
            ConvertItem<FName>("Gender", x => Gender = x.Name);
            ConvertItem<FName>("KillerHeight", x => KillerHeight = x.Name);
            ConvertItem<FName>("IconFilePath", x => IconPath = x.Name);
            ConvertItem<FName>("BackgroundImagePath", x => BackgroundPath = x.Name);
            ConvertItem<List<Property>>("KillerAbilities", x => KillerAbilities = x.Select(y => ((FName)y.Value).Name.Value).ToArray());
            ConvertItem<TaggedObject>("SlideShowDescriptions", x =>
            {
                var overviewStr = "";
                var playStyleStr = "";

                var item = x.Vars.FirstOrDefault(y => y.Key == "Overview");
                if (!item.Equals(default) && item.Value.Value is FText overview)
                    overviewStr = overview.ToString();

                item = x.Vars.FirstOrDefault(y => y.Key == "Playstyle");
                if (!item.Equals(default) && item.Value.Value is FText playStyle)
                    playStyleStr = playStyle.ToString();

                SlideShowDescriptions = new SlideDescription()
                {
                    Overview = overviewStr,
                    Playstyle = playStyleStr
                };
            });
            ConvertItem<TaggedObject>("TunableDB", x =>
            {
                var item = x.Vars.FirstOrDefault(y => y.Key == "AssetPtr");
                if (!item.Equals(default) && item.Value.Value is var softPtr)
                    TunablePath = softPtr.ToString();
            });
        }
    }
}
