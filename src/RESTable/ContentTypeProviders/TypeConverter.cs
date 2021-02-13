using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RESTable.ContentTypeProviders
{
    internal class TypeConverter : JsonConverter<Type>
    {
        public override bool CanRead { get; } = false;
        public override void WriteJson(JsonWriter writer, Type value, JsonSerializer _) => writer.WriteValue(value.GetRESTableTypeName());
        public override Type ReadJson(JsonReader r, Type o, Type e, bool h, JsonSerializer _) => throw new NotImplementedException();
    }

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