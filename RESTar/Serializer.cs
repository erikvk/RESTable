using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using RESTar.View.Serializer;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
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

        static Serializer()
        {
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
                ContractResolver = new DefaultResolver(),
                DateFormatHandling = IsoDateFormat,
                DateTimeZoneHandling = Utc
            };
            var enumConverter = new StringEnumConverter();
            Settings.Converters.Add(enumConverter);
            VmSettings.Converters.Add(enumConverter);
            JsonSerializer = JsonSerializer.Create(Settings);
        }

        /// <summary>
        /// Serializes the object to JSON
        /// </summary>
        public static string ToJSON(this object value) => value.Serialize();

        /// <summary>
        /// Deserializes the given JSON string to an object of the given type
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        public static T FromJSON<T>(this string json) => json.Deserialize<T>();

        internal static string Serialize(this object value, Type type = null)
        {
            return SerializeObject(value, type, _PrettyPrint ? Indented : Formatting.None, Settings);
        }

        internal static dynamic Deserialize(this string json, Type type) => DeserializeObject(json, type);
        internal static JToken Deserialize(this string json) => DeserializeObject<JToken>(json);
        internal static T Deserialize<T>(this string json) => DeserializeObject<T>(json);
        internal static void Populate(string json, object target) => PopulateObject(json, target, Settings);
        internal static string SerializeToViewModel(this object value) => SerializeObject(value, VmSettings);
    }

    internal static class XmlSerializer
    {
        internal static string SerializeXml(this XmlDocument xml)
        {
            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter))
            {
                xml.WriteTo(xmlTextWriter);
                xmlTextWriter.Flush();
                return stringWriter.GetStringBuilder().ToString();
            }
        }
    }

    internal static class ExcelSerializer
    {
        internal static DataSet ToDataSet(this IEnumerable<dynamic> list, Internal.IResource resource)
        {
            var ds = new DataSet();
            ds.Tables.Add(list.MakeTable(resource));
            return ds;
        }
    }
}