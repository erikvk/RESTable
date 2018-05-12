using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Linq;
using RESTar.Requests;

namespace RESTar.ContentTypeProviders.NativeJsonProtocol
{
    internal class HeadersConverter : JsonConverter
    {
        /// <inheritdoc />
        /// <summary>
        /// Writes only custom headers to JSON
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var headers = value as Headers;
            var jobj = new JObject();
            headers?.CustomHeaders.ForEach(pair => jobj[pair.Key] = pair.Value);
            jobj.WriteTo(writer);
        }

        /// <inheritdoc />
        /// <summary>
        /// Reads only custom headers from JSON
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            IEnumerable<KeyValuePair<string, JToken>> values = JObject.Load(reader);
            if (!(existingValue is Headers headers)) headers = new Headers();
            values.Where(pair => Headers.IsCustom(pair.Key)).ForEach(pair => headers[pair.Key] = pair.Value.ToObject<string>());
            return headers;
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(Headers);
    }
}