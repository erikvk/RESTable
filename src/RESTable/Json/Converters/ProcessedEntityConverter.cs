using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using RESTable.ContentTypeProviders;
using RESTable.Meta;
using RESTable.Requests.Processors;

namespace RESTable.Json.Converters
{
    [BuiltInConverter]
    public class ProcessedEntityConverter : JsonConverter<ProcessedEntity>
    {
        private ISerializationMetadata<ProcessedEntity> Metadata { get; }
        private IJsonProvider JsonProvider { get; }

        internal ProcessedEntityConverter(ISerializationMetadata<ProcessedEntity> metadata, IJsonProvider jsonProvider)
        {
            Metadata = metadata;
            JsonProvider = jsonProvider;
        }

        public override ProcessedEntity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, ProcessedEntity value, JsonSerializerOptions options)
        {
            var jsonWriter = JsonProvider.GetJsonWriter(writer, options);
            jsonWriter.WriteDictionary(value, Metadata);
        }
    }
}