using System.Collections.Generic;
using System.Linq;
using UnrealTools.Core;

using TaggedItem = System.Collections.Generic.KeyValuePair<string, UnrealTools.Objects.Interfaces.IProperty>;
using Property = UnrealTools.Objects.Interfaces.IProperty;

namespace DBD_API.Modules.DbD.Items
{
    using TaggedItemsList = System.Collections.Generic.List<TaggedItem>;

    public class PerkInfo : BaseItem
    {
        public int AssociatedPlayerIndex { get; private set; }
        public int MandatoryOnBloodweblevel { get; private set; }
        public int TeachableOnBloodweblevel { get; private set; }
        public int AtlantaTeachableLevel { get; private set; }
        public string[] PerkCategory { get; private set; }
        public string[] PerkLevelRarity { get; private set; }
        public string PerkDefaultDescription { get; private set; }
        public string PerkLevel1Description { get; private set; }
        public string PerkLevel2Description { get; private set; }
        public string PerkLevel3Description { get; private set; }
        public string PerkUnlicensedDescriptionOverride { get; private set; }


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
        }
    };
}
