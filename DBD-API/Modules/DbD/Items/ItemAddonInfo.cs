using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnrealTools.Core;
using TaggedItem = System.Collections.Generic.KeyValuePair<string, UnrealTools.Objects.Interfaces.IProperty>;
using Property = UnrealTools.Objects.Interfaces.IProperty;

namespace DBD_API.Modules.DbD.Items
{
    using TaggedItemsList = System.Collections.Generic.List<TaggedItem>;
    public class ItemAddonInfo : BaseItem
    {
        public string[] ParentItems { get; private set; }

        public ItemAddonInfo(TaggedItemsList itemList) : base(itemList)
        {
            ConvertItem<List<Property>>("ParentItems", x => ParentItems = x.Select(x => (x.Value as FName)?.Name.Value).ToArray());
        }
    }
}