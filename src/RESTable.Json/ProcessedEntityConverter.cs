using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using RESTable.Requests.Processors;

namespace RESTable.Json
{
    public class ProcessedEntityConverter : JsonConverter<ProcessedEntity>
    {
        public override ProcessedEntity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, ProcessedEntity value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            DefaultConverterOperations.WriteDynamicMembers<ProcessedEntity, string, object?>(writer, value, options);
            writer.WriteEndObject();
        }
    }
}