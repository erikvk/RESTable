using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RESTable.Requests;

namespace RESTable.Json
{
    internal class HeadersConverter : JsonConverter<Headers>
    {
        private HashSet<string> WhitelistedNonCustomHeaders { get; }

        public HeadersConverter() : this(false) { }

        public HeadersConverter(bool allowAuth)
        {
            WhitelistedNonCustomHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (allowAuth)
                WhitelistedNonCustomHeaders.Add(nameof(Headers.Authorization));
        }

        /// <inheritdoc />
        /// <summary>
        /// Reads only custom headers from JSON
        /// </summary>
        public override Headers Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var headers = new Headers();
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
            var values = jsonElement.EnumerateObject();
            var headersToRead = values.Where(pair => WhitelistedNonCustomHeaders.Contains(pair.Name) || pair.Name.IsCustomHeaderName());
            foreach (var pair in headersToRead)
                headers[pair.Name] = pair.Value.ToObject<string>(options);
            return headers;
        }

        /// <inheritdoc />
        /// <summary>
        /// Writes only custom headers to JSON
        /// </summary>s
        public override void Write(Utf8JsonWriter writer, Headers headers, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            if (headers is IHeadersInternal headersInternal)
            {
                foreach (var (key, value) in headersInternal.GetCustom(WhitelistedNonCustomHeaders))
                {
                    writer.WriteString(key, value);
                }
            }
            writer.WriteEndObject();
        }
    }
}