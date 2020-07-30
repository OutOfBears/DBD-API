using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TaggedItem = System.Collections.Generic.KeyValuePair<string, UETools.Objects.Interfaces.IProperty>;
using Property = UETools.Objects.Interfaces.IProperty;

namespace DBD_API.Modules.DbD.PakItems
{
    using TaggedItemsList = System.Collections.Generic.List<TaggedItem>;

    public class BaseInfo
    {
        protected readonly TaggedItemsList _itemList;

        public BaseInfo(TaggedItemsList itemList)
        {
            _itemList = itemList;
        }

        public TaggedItemsList GetAll()
        {
            return _itemList;
        }

        protected void ConvertItem<T>(string name, Action<T> callback)
        {
            var item = _itemList.FirstOrDefault(x => x.Key == name);
            if (item.Equals(default) || !(item.Value is Property value) || !(value.Value is T itemValue))
                return;

            callback(itemValue);
        }
    }
}
