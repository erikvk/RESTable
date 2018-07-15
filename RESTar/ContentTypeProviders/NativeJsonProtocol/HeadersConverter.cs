using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Linq;
using RESTar.Requests;

namespace RESTar.ContentTypeProviders.NativeJsonProtocol
{
    internal class HeadersConverter : JsonConverter<Headers>
    {
        private HashSet<string> WhitelistedNonCustomHeaders { get; }

        public HeadersConverter(params string[] whitelisted)
        {
            WhitelistedNonCustomHeaders = new HashSet<string>(whitelisted, StringComparer.OrdinalIgnoreCase);
            if (WhitelistedNonCustomHeaders.Contains("*"))
                WhitelistedNonCustomHeaders.UnionWith(Headers.NonCustomHeaders);
        }

        /// <inheritdoc />
        /// <summary>
        /// Writes only custom headers to JSON
        /// </summary>
        public override void WriteJson(JsonWriter writer, Headers headers, JsonSerializer s)
        {
            var jobj = new JObject();
            headers?.GetCustom(WhitelistedNonCustomHeaders).ForEach(pair => jobj[pair.Key] = pair.Value);
            jobj.WriteTo(writer);
        }

        /// <inheritdoc />
        /// <summary>
        /// Reads only custom headers from JSON
        /// </summary>
        public override Headers ReadJson(JsonReader reader, Type o, Headers headers, bool h, JsonSerializer s)
        {
            IEnumerable<KeyValuePair<string, JToken>> values = JObject.Load(reader);
            headers = headers ?? new Headers();
            values.Where(pair => WhitelistedNonCustomHeaders.Contains(pair.Key) || Headers.IsCustom(pair.Key))
                .ForEach(pair => headers[pair.Key] = pair.Value.ToObject<string>());
            return headers;
        }
    }
}