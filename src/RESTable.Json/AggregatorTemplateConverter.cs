using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RESTable.Json
{
    internal class AggregatorTemplateConverter : CustomCreationConverter<Aggregator>
    {
        public override Aggregator Create(Type objectType) => new();

        public override bool CanConvert(Type objectType) => objectType == typeof(object) || base.CanConvert(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                case JsonToken.StartObject:
                    return base.ReadJson(reader, objectType, existingValue, serializer);
                default: return serializer.Deserialize(reader);
            }
        }
    }
}