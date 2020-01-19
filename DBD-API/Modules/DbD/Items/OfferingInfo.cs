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
    public class OfferingInfo : BaseItem
    {
        public string OfferingType { get; private set; }
        public bool CanUseAfterEventEnd { get; private set; }
        
        public OfferingInfo(TaggedItemsList itemList) : base(itemList) 
        {
            ConvertItem<FName>("OfferingType", x => OfferingType = x.Name);
            ConvertItem<bool>("CanUseAfterEventEnd", x => CanUseAfterEventEnd = x);
        }
    }
}
