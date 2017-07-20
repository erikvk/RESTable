using System.Collections.Generic;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using RESTar.Serialization;
using static Newtonsoft.Json.DateFormatHandling;
using static Newtonsoft.Json.DateTimeZoneHandling;
using static Newtonsoft.Json.Formatting;
using static Newtonsoft.Json.DateParseHandling;
using static Newtonsoft.Json.FloatParseHandling;
using static Newtonsoft.Json.NullValueHandling;
using static Newtonsoft.Json.JsonConvert;
using static RESTar.Settings;
using Type = System.Type;
using Formatting = Newtonsoft.Json.Formatting;

namespace RESTar
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
                DateParseHandling = DateTime,
                DateFormatHandling = IsoDateFormat,
                DateTimeZoneHandling = Utc,
                ContractResolver = new DefaultResolver(),
                NullValueHandling = Include,
                FloatParseHandling = Decimal
            };
            VmSettings = new JsonSerializerSettings
            {
                ContractResolver = new CreateViewModelResolver(),
                DateFormatHandling = IsoDateFormat,
                DateTimeZoneHandling = Utc
            };
            var enumConverter = new StringEnumConverter();
            Settings.Converters.Add(enumConverter);
            VmSettings.Converters.Add(enumConverter);
            JsonSerializer = JsonSerializer.Create(Settings);
        }

        internal static string Serialize(this object value, Type type = null)
        {
            return SerializeObject(value, type, _PrettyPrint ? Indented : Formatting.None, Settings);
        }

        internal static void Populate(JToken value, object target)
        {
            using (var sr = value.CreateReader())
                JsonSerializer.Populate(sr, target);
        }

        internal static dynamic Deserialize(this string json, Type type) => DeserializeObject(json, type);
        internal static JToken Deserialize(this string json) => DeserializeObject<JToken>(json);
        internal static T Deserialize<T>(this string json) => DeserializeObject<T>(json);
        internal static void Populate(string json, object target) => PopulateObject(json, target, Settings);
        internal static string SerializeToViewModel(this object value) => SerializeObject(value, VmSettings);

        internal static string SerializeXML<T>(this IEnumerable<T> data)
        {
            var json = data.Serialize();
            var xml = DeserializeXmlNode($@"{{""row"":{json}}}", "root", true);
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