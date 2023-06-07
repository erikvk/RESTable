using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using RESTable.ContentTypeProviders;
using RESTable.Meta;

namespace RESTable.Json.Converters;

/// <summary>
///     Converter for declared types that are dictionaries, defined as implementing IEnumerable{T} where
///     T is some KeyValuePair{TKey,TValue} type.
///     member.
/// </summary>
[BuiltInConverter]
public class DefaultDictionaryConverter<TDictionary, TValue> : JsonConverter<TDictionary> where TDictionary : IDictionary<string, TValue?>
{
    public DefaultDictionaryConverter(ISerializationMetadata<TDictionary> metadata, IJsonProvider jsonProvider)
    {
        Metadata = metadata;
        JsonProvider = jsonProvider;
    }

    private ISerializationMetadata<TDictionary> Metadata { get; }
    private IJsonProvider JsonProvider { get; }

    public override bool HandleNull => true;

    public override TDictionary? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonReader = JsonProvider.GetJsonReader(options);
        return jsonReader.ReadToDictionaryOrGetDefault<TDictionary, TValue>(ref reader, Metadata);
    }

    public override void Write(Utf8JsonWriter writer, TDictionary? value, JsonSerializerOptions options)
    {
        var jsonWriter = JsonProvider.GetJsonWriter(writer, options);
        jsonWriter.WriteDictionary(value, Metadata);
    }
}
