using System.Collections.Generic;
using System.IO;
using System;
using System.Text;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using RESTar.ContentTypeProviders;
using RESTar.Serialization.NativeProtocol;
using static Newtonsoft.Json.Formatting;
using static RESTar.Admin.Settings;

namespace RESTar.Serialization
{
    /// <summary>
    /// The serializer for the RESTar instance
    /// </summary>
    public static class Serializer
    {
        private static readonly JsonSerializerSettings VmSettings;

        /// <summary>
        /// The settings that are used when serializing and deserializing JSON
        /// </summary>
        public static readonly JsonSerializerSettings Settings;

        /// <summary>
        /// The JSON serializer
        /// </summary>
        public static readonly JsonSerializer Json;

        /// <summary>
        /// The XML serializer
        /// </summary>
        public static readonly XmlSerializer XML;

        /// <summary>
        /// UTF 8 encoding without byte order mark (BOM)
        /// </summary>
        public static readonly Encoding UTF8;

        /// <summary>
        /// A statically accessable JsonContentProvider
        /// </summary>
        public static readonly JsonContentProvider JsonProvider;

        /// <summary>
        /// A statically accessable ExcelContentProvider
        /// </summary>
        public static readonly ExcelContentProvider ExcelProvider;

        static Serializer()
        {
            Settings = new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.DateTime,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                ContractResolver = new DefaultResolver(),
                NullValueHandling = NullValueHandling.Include,
                FloatParseHandling = FloatParseHandling.Decimal,
            };
            VmSettings = new JsonSerializerSettings
            {
                ContractResolver = new CreateViewModelResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };

            var enumConverter = new StringEnumConverter();
            var headersConverter = new HeadersConverter();
            var ddictionaryConverter = new DDictionaryConverter();
            Settings.Converters.Add(enumConverter);
            Settings.Converters.Add(headersConverter);
            Settings.Converters.Add(ddictionaryConverter);
            VmSettings.Converters.Add(enumConverter);
            VmSettings.Converters.Add(headersConverter);
            VmSettings.Converters.Add(ddictionaryConverter);
            Json = JsonSerializer.Create(Settings);
            XML = new XmlSerializer(typeof(List<object>));
            UTF8 = new UTF8Encoding(false);
            JsonProvider = new JsonContentProvider();
            ExcelProvider = new ExcelContentProvider();
        }

        #region Main serializers

        internal static string SerializeFormatter(this JToken formatterToken, out int indents)
        {
            using (var sw = new StringWriter())
            using (var jwr = new FormatWriter(sw))
            {
                Json.Formatting = Indented;
                Json.Serialize(jwr, formatterToken);
                indents = jwr.Depth;
                return sw.ToString();
            }
        }

        #endregion

        internal static string GetString(this Stream stream)
        {
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        internal static string Serialize(this object value, Type type = null)
        {
            return JsonConvert.SerializeObject(value, type, _PrettyPrint ? Indented : None, Settings);
        }

        internal static void Populate(JToken value, object target)
        {
            using (var sr = value.CreateReader())
                Json.Populate(sr, target);
        }

        internal static JToken ToJToken(this object o) => JToken.FromObject(o, Json);

        internal static T Deserialize<T>(this string json) => JsonConvert.DeserializeObject<T>(json);


        internal static string SerializeToViewModel(this object value)
        {
            return JsonConvert.SerializeObject(value, VmSettings);
        }
    }
}