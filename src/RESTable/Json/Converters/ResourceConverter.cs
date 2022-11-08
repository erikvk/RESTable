using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using RESTable.Meta;

namespace RESTable.Json.Converters;

[BuiltInConverter]
public class ResourceConverter<T> : JsonConverter<T> where T : IResource
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Name);
    }
}
