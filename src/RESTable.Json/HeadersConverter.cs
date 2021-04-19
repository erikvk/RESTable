using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        /// Writes only custom headers to JSON
        /// </summary>s
        public override void WriteJson(JsonWriter writer, Headers headers, JsonSerializer s)
        {
            var jobj = new JObject();

            if (headers is IHeadersInternal headersInternal)
            {
                foreach (var (key, value) in headersInternal.GetCustom(WhitelistedNonCustomHeaders))
                    jobj[key] = value;
            }

            jobj.WriteTo(writer);
        }

        /// <inheritdoc />
        /// <summary>
        /// Reads only custom headers from JSON
        /// </summary>
        public override Headers ReadJson(JsonReader reader, Type o, Headers headers, bool h, JsonSerializer s)
        {
            IEnumerable<KeyValuePair<string, JToken>> values = JObject.Load(reader);
            headers ??= new Headers();
            var headersToRead = values.Where(pair => WhitelistedNonCustomHeaders.Contains(pair.Key) || pair.Key.IsCustomHeaderName());
            foreach (var (key, value) in headersToRead)
                headers[key] = value.ToObject<string>();
            return headers;
        }
    }
}