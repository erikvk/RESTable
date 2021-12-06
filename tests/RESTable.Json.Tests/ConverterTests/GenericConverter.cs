using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using RESTable.ContentTypeProviders;
using RESTable.Meta;

namespace RESTable.Json.Tests;

public class GenericConverter<T> : JsonConverter<T> where T : class, IToConvert
{
    public GenericConverter(ISerializationMetadata<T> metadata, IJsonProvider jsonProvider)
    {
        Metadata = metadata;
        JsonProvider = jsonProvider;
    }

    private ISerializationMetadata<T> Metadata { get; }
    private IJsonProvider JsonProvider { get; }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonReader = JsonProvider.GetJsonReader(options);
        return jsonReader.ReadToObjectOrGetDefault(ref reader, Metadata);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }
        var jsonWriter = JsonProvider.GetJsonWriter(writer, options);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("GenericConverter", typeof(T).Name);
        jsonWriter.WriteDeclaredMembers(value, Metadata);
        jsonWriter.WriteEndObject();
    }
}