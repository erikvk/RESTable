using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using RESTable.ContentTypeProviders;
using RESTable.Meta;

namespace RESTable.Json.Converters
{
    /// <summary>
    /// Converter for declared types that are dictionaries, defined as implementing IEnumerable{T} where
    /// T is some KeyValuePair{TKey,TValue} type.
    /// member.
    /// </summary>
    [BuiltInConverter]
    public class DefaultReadonlyDictionaryConverter<T, TKey, TValue> : JsonConverter<T> where T : ICollection<KeyValuePair<TKey, TValue?>> where TKey : notnull
    {
        private ISerializationMetadata<T> Metadata { get; }
        private IJsonProvider JsonProvider { get; }

        public DefaultReadonlyDictionaryConverter(ISerializationMetadata<T> metadata, IJsonProvider jsonProvider)
        {
            Metadata = metadata;
            JsonProvider = jsonProvider;
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var jsonReader = JsonProvider.GetJsonReader(options);
            return jsonReader.ReadToObjectOrGetDefault(ref reader, Metadata);
        }

        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            var jsonWriter = JsonProvider.GetJsonWriter(writer, options);
            jsonWriter.WriteDictionary(value, Metadata);
        }

        public override bool HandleNull => true;
    }
}