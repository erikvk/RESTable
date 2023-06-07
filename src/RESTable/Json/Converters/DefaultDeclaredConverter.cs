using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using RESTable.ContentTypeProviders;
using RESTable.Meta;

namespace RESTable.Json.Converters;

/// <summary>
///     Converter for declared types that have at least one RESTableMemberAttribute on some
///     member.
/// </summary>
/// <typeparam name="T"></typeparam>
[BuiltInConverter]
public class DefaultDeclaredConverter<T> : JsonConverter<T>
{
    public DefaultDeclaredConverter(ISerializationMetadata<T> metadata, IJsonProvider jsonProvider)
    {
        Metadata = metadata;
        JsonProvider = jsonProvider;
    }

    private ISerializationMetadata<T> Metadata { get; }
    private IJsonProvider JsonProvider { get; }

    public override bool HandleNull => true;

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonReader = JsonProvider.GetJsonReader(options);
        return jsonReader.ReadToObjectOrGetDefault(ref reader, Metadata);
    }

    public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
    {
        var jsonWriter = JsonProvider.GetJsonWriter(writer, options);
        jsonWriter.WriteObject(value, Metadata);
    }
}
