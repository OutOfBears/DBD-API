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
    public class OfferingInfo : BaseItem
    {
        [JsonPropertyName("offeringType")]
        public string OfferingType { get; private set; }

        [JsonPropertyName("canUseAfterEventEnd")]
        public bool CanUseAfterEventEnd { get; private set; }
        
        public OfferingInfo(TaggedItemsList itemList) : base(itemList) 
        {
            ConvertItem<FName>("OfferingType", x => OfferingType = x.Name);
            ConvertItem<bool>("CanUseAfterEventEnd", x => CanUseAfterEventEnd = x);
        }
    }
}
