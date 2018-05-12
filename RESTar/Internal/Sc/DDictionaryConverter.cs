using System;
using Dynamit;
using Newtonsoft.Json;
using Starcounter;

namespace RESTar.Internal.Sc
{
    internal class DDictionaryConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dict = (DDictionary) value;
            writer.WriteStartObject();
            foreach (var pair in dict.KeyValuePairs)
            {
                writer.WritePropertyName(pair.Key);
                writer.WriteValue(pair.Value);
            }
            writer.WritePropertyName("$ObjectNo");
            writer.WriteValue(dict.GetObjectNo());
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
            throw new NotImplementedException();

        public override bool CanRead { get; } = false;
        public override bool CanConvert(Type objectType) => objectType.IsSubclassOf(typeof(DDictionary));
    }
}