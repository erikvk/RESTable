using System.Collections.Generic;
using System.IO;
using System.Xml;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using static RESTar.Admin.Settings;
using Formatting = Newtonsoft.Json.Formatting;

namespace RESTar.Serialization
{
    /// <summary>
    /// The serializer for the RESTar instance
    /// </summary>
    public static class Serializer
    {
        private static readonly JsonSerializerSettings VmSettings;
        internal static readonly JsonSerializerSettings Settings;
        internal static readonly JsonSerializer JsonSerializer;
        internal static readonly XmlWriterSettings XMLIndentSettings;

        static Serializer()
        {
            XMLIndentSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            Settings = new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.DateTime,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                ContractResolver = new DefaultResolver(),
                NullValueHandling = NullValueHandling.Include,
                FloatParseHandling = FloatParseHandling.Decimal
            };
            VmSettings = new JsonSerializerSettings
            {
                ContractResolver = new CreateViewModelResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };
            var enumConverter = new StringEnumConverter();
            Settings.Converters.Add(enumConverter);
            VmSettings.Converters.Add(enumConverter);
            JsonSerializer = JsonSerializer.Create(Settings);
        }

        internal static string Serialize(this object value, Type type = null)
        {
            return JsonConvert.SerializeObject(value, type, _PrettyPrint ? Formatting.Indented : Formatting.None, Settings);
        }

        internal static void Populate(JToken value, object target)
        {
            using (var sr = value.CreateReader())
                JsonSerializer.Populate(sr, target);
        }

        internal static JToken ToJToken(this object o) => JToken.FromObject(o, JsonSerializer);

        internal static dynamic Deserialize(this string json, Type type) => JsonConvert.DeserializeObject(json, type);
        internal static JToken Deserialize(this string json) => JsonConvert.DeserializeObject<JToken>(json);
        internal static T Deserialize<T>(this string json) => JsonConvert.DeserializeObject<T>(json);
        internal static void Populate(string json, object target) => JsonConvert.PopulateObject(json, target, Settings);
        internal static string SerializeToViewModel(this object value) => JsonConvert.SerializeObject(value, VmSettings);

        internal static string SerializeXML<T>(this IEnumerable<T> data)
        {
            var json = data.Serialize();
            if (json == "[]") return null;
            var xml = JsonConvert.DeserializeXmlNode($@"{{""row"":{json}}}", "root", true);
            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter, _PrettyPrint ? XMLIndentSettings : null))
            {
                xml.WriteTo(xmlTextWriter);
                xmlTextWriter.Flush();
                return stringWriter.GetStringBuilder().ToString();
            }
        }
    }
}