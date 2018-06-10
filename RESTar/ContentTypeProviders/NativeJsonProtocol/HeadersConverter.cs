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
        /// <inheritdoc />
        /// <summary>
        /// Writes only custom headers to JSON
        /// </summary>
        public override void WriteJson(JsonWriter writer, Headers headers, JsonSerializer serializer)
        {
            var jobj = new JObject();
            headers?.CustomHeaders.ForEach(pair => jobj[pair.Key] = pair.Value);
            jobj.WriteTo(writer);
        }

        /// <inheritdoc />
        /// <summary>
        /// Reads only custom headers from JSON
        /// </summary>
        public override Headers ReadJson(JsonReader reader, Type objectType, Headers headers, bool hasExistingValue, JsonSerializer serializer)
        {
            IEnumerable<KeyValuePair<string, JToken>> values = JObject.Load(reader);
            headers = headers ?? new Headers();
            values.Where(pair => Headers.IsCustom(pair.Key)).ForEach(pair => headers[pair.Key] = pair.Value.ToObject<string>());
            return headers;
        }
    }
}