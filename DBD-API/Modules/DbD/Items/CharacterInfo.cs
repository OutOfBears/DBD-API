using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnrealTools.Core;
using UnrealTools.Objects.Classes;
using TaggedItem = System.Collections.Generic.KeyValuePair<string, UnrealTools.Objects.Interfaces.IProperty>;
using Property = UnrealTools.Objects.Interfaces.IProperty;

namespace DBD_API.Modules.DbD.Items
{
    using TaggedItemsList = System.Collections.Generic.List<TaggedItem>;

    public struct SlideDescription
    {
        public string Overview { get; set; }
        public string Playstyle { get; set; }
    }

    public class CharacterInfo : BaseInfo
    {
        public int CharacterIndex { get; private set; }
        public string Role { get; private set; }
        public string Difficulty { get; private set; }
        public string DisplayName { get; private set; }
        public string Backstory { get; private set; }
        public string Biography { get; private set; }
        public string RequiredDlcIdString { get; private set; }
        public string IdName { get; private set; }
        public string DefaultItem { get; private set; }
        public bool IsAvailableInNonViolentBuild { get; private set; }
        public bool IsAvailableInAtlantaBuild { get; private set; }
        public uint PlatformExclusiveFlag { get; private set; }
        public string[] KillerAbilities { get; private set; }
        public string Gender { get; private set; }
        public string KillerHeight { get; private set; }
        public SlideDescription SlideShowDescriptions { get; private set; }

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
        }
    }
}
