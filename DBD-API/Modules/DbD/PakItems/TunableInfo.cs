using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UETools.Core;
using UETools.Objects.Classes;
using TaggedItem = System.Collections.Generic.KeyValuePair<string, UETools.Objects.Classes.TaggedObject>;

namespace DBD_API.Modules.DbD.PakItems
{
    using TaggedItemsList = System.Collections.Generic.List<TaggedItem>;
    using TunableInfos = ConcurrentDictionary<string, TunableInfo>;

    public class TunableContainer
    {
        [JsonPropertyName("baseTunables")]
        public TunableInfos BaseTunables { get; private set; }

        [JsonPropertyName("killerTunables")]
        public TunableInfos KillerTunables { get; private set; }

        [JsonPropertyName("knownTunableValues")]
        public TunableInfos KnownTunableValues { get; private set; }

        [JsonPropertyName("unknownTunableValues")]
        public TunableInfos UnknownTunableValues { get; private set; }

        public TunableContainer()
        {
            BaseTunables = new TunableInfos();
            KillerTunables = new TunableInfos();
            KnownTunableValues = new TunableInfos();
            UnknownTunableValues = new TunableInfos();
        }
    }

    public class Tunable
    {
        [JsonPropertyName("value")]
        public object Value { get; private set; }

        [JsonPropertyName("atlantaOverridenValue")]
        public object AtlantaOverridenValue { get; private set; }

        [JsonPropertyName("description")]
        public string Description { get; private set; }

        [JsonPropertyName("descriptorTags")]
        public string DescriptorTags { get; private set; }

        [JsonPropertyName("overridenInAtlanta")]
        public bool OverridenInAtlanta { get; private set; }

        public Tunable(object value, object atlantaValue, string description,
            string descTags, bool overridden)
        {
            Value = value;
            AtlantaOverridenValue = atlantaValue;
            Description = description;
            DescriptorTags = descTags;
            OverridenInAtlanta = overridden;
        }
    }

    [JsonConverter(typeof(TunableSerializer))]
    public class TunableInfo
    {
        public ConcurrentDictionary<string, Tunable> Tunables { get; private set; }

        public TunableInfo(TaggedItemsList itemList)
        {
            Tunables = new ConcurrentDictionary<string, Tunable>();

            foreach (var item in itemList)
            {
                if (item.Value == null || string.IsNullOrEmpty(item.Key))
                    continue;

                object value = null;
                object atlantaValue = null;
                var description = "";
                var descriptorTags = "";
                var overriden = false;

                var obj = item.Value;
                var temp = obj.Vars.FirstOrDefault(x => x.Key == "Value");
                if (!temp.Equals(default) && temp.Value != null) value = temp.Value.Value;

                temp = obj.Vars.FirstOrDefault(x => x.Key == "AtlantaOverriddenValue");
                if (!temp.Equals(default) && temp.Value != null) atlantaValue = temp.Value.Value;

                temp = obj.Vars.FirstOrDefault(x => x.Key == "Description");
                if (!temp.Equals(default) && temp.Value != null && temp.Value.Value is FString desc)
                    description = desc.Value;

                temp = obj.Vars.FirstOrDefault(x => x.Key == "DescriptorTags");
                if (!temp.Equals(default) && temp.Value != null && temp.Value.Value is FString descTags)
                    descriptorTags = descTags.Value;

                temp = obj.Vars.FirstOrDefault(x => x.Key == "DescriptorTags");
                if (!temp.Equals(default) && temp.Value != null && temp.Value.Value is bool overridenval)
                    overriden = overridenval;

                Tunables[item.Key] = new Tunable(value, atlantaValue, description, descriptorTags, overriden);
            }
        }
    }

    public class TunableSerializer : JsonConverter<TunableInfo>
    {
        public override TunableInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, TunableInfo value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var item in value.Tunables)
            {
                if (options.IgnoreNullValues && item.Value == null)
                    continue;

                writer.WritePropertyName(item.Key);
                JsonSerializer.Serialize(writer, item.Value, options);

            }
            writer.WriteEndObject();
        }
    }
}
