using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RESTable.Json
{
    internal class AsyncEnumerableConverter : JsonConverter
    {
        public override bool CanRead => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var asyncEnumerable = (IAsyncEnumerable<object>) value;
            writer.WriteStartArray();
            foreach (var item in asyncEnumerable.ToEnumerable())
            {
                serializer.Serialize(writer, item);
            }
            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }
    }
}