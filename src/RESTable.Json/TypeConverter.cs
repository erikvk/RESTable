using System;
using Newtonsoft.Json;

namespace RESTable.Json
{
    internal class TypeConverter : JsonConverter<Type>
    {
        public override bool CanRead { get; } = false;
        public override void WriteJson(JsonWriter writer, Type value, JsonSerializer _) => writer.WriteValue(value.GetRESTableTypeName());
        public override Type ReadJson(JsonReader r, Type o, Type e, bool h, JsonSerializer _) => throw new NotImplementedException();
    }
}