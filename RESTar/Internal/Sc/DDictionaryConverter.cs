using System;
using Dynamit;
using Newtonsoft.Json;
using Starcounter;

namespace RESTar.Internal.Sc
{
    internal class DDictionaryConverter : JsonConverter<DDictionary>
    {
        public override bool CanRead { get; } = false;
        public override DDictionary ReadJson(JsonReader r, Type o, DDictionary e, bool h, JsonSerializer _) => throw new NotImplementedException();

        public override void WriteJson(JsonWriter writer, DDictionary dictionary, JsonSerializer _)
        {
            writer.WriteStartObject();
            foreach (var pair in dictionary.KeyValuePairs)
            {
                writer.WritePropertyName(pair.Key);
                writer.WriteValue(pair.Value);
            }
            writer.WritePropertyName("$ObjectNo");
            writer.WriteValue(dictionary.GetObjectNo());
            writer.WriteEndObject();
        }
    }
}