using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
    internal static class JsonSerializer
    {
        private static readonly JsonSerializerSettings VmSettings;
        internal static readonly JsonSerializerSettings Settings;

        static JsonSerializer()
        {
            Settings = new JsonSerializerSettings
            {
                DateParseHandling = DateTime,
                DateFormatHandling = IsoDateFormat,
                DateTimeZoneHandling = Utc,
                ContractResolver = _CamelCase
                    ? new CamelCasePropertyNamesContractResolver()
                    : new DefaultContractResolver(),
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
        }

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