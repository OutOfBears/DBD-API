using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UETools.Core;
using UETools.Objects.Classes;
using TaggedItem = System.Collections.Generic.KeyValuePair<string, UETools.Objects.Interfaces.IProperty>;
using Property = UETools.Objects.Interfaces.IProperty;

namespace DBD_API.Modules.DbD.PakItems
{
    using TaggedItemsList = System.Collections.Generic.List<TaggedItem>;
    public class ItemAddonInfo : BaseItem
    {
        [JsonPropertyName("parentItems")]
        public string[] ParentItems { get; private set; }

        public ItemAddonInfo(TaggedItemsList itemList) : base(itemList)
        {
            ConvertItem<List<Property>>("ParentItems", x => ParentItems = x.Select(x => (x.Value as FName)?.Name.Value).ToArray());
        }
    }
}