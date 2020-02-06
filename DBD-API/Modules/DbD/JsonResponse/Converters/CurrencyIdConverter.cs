using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DBD_API.Modules.DbD;

namespace DBD_API.Modules.DbD.JsonResponse.Converters
{
    internal class CurrencyIdConverter : JsonConverter<CurrencyId>
    {
        public override bool CanConvert(Type t) => t == typeof(CurrencyId) || t == typeof(CurrencyId?);

        public override CurrencyId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            switch (value)
            {
                case "Cells":
                    return CurrencyId.Cells;
                case "HalloweenEventCurrency":
                    return CurrencyId.HalloweenEventCurrency;
                case "LunarNewYearCoins":
                    return CurrencyId.LunarNewYearCoins;
                case "Shards":
                    return CurrencyId.Shards;
            }

            throw new Exception("Cannot unmarshal type CurrencyId");
        }

        public override void Write(Utf8JsonWriter writer, CurrencyId value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case CurrencyId.Cells:
                    writer.WriteStringValue("Cells");
                    return;
                case CurrencyId.HalloweenEventCurrency:
                    writer.WriteStringValue("HalloweenEventCurrency");
                    return;
                case CurrencyId.LunarNewYearCoins:
                    writer.WriteStringValue("LunarNewYearCoins");
                    return;
                case CurrencyId.Shards:
                    writer.WriteStringValue("Shards");
                    return;
            }

            throw new Exception("Cannot marshal type CurrencyId");
        }

        public static readonly CurrencyIdConverter Singleton = new CurrencyIdConverter();
    }
}
