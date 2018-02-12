using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using System.Text;
using ExcelDataReader;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using RESTar.Results.Error.BadRequest;
using RESTar.Serialization.NativeProtocol;
using static System.Linq.Enumerable;
using static Newtonsoft.Json.Formatting;
using static RESTar.Admin.Settings;
using static RESTar.Methods;

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
        /// UTF 8 encoding without byte order mark (BOM)
        /// </summary>
        public static readonly Encoding UTF8;

        static Serializer()
        {
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
            var headersConverter = new HeadersConverter();
            var ddictionaryConverter = new DDictionaryConverter();
            Settings.Converters.Add(enumConverter);
            Settings.Converters.Add(headersConverter);
            Settings.Converters.Add(ddictionaryConverter);
            VmSettings.Converters.Add(enumConverter);
            VmSettings.Converters.Add(headersConverter);
            VmSettings.Converters.Add(ddictionaryConverter);
            Json = JsonSerializer.Create(Settings);
            UTF8 = new UTF8Encoding(false);
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

        internal static bool SerializeOutputExcel
        (
            this IEnumerable<object> data,
            IEntityResource resource,
            out MemoryStream stream,
            out long count
        )
        {
            try
            {
                stream = null;
                var excel = data.ToExcel(resource);
                count = excel?.Worksheet(1)?.RowsUsed().Count() - 1 ?? 0L;
                if (excel == null) return false;
                stream = new MemoryStream();
                excel.SaveAs(stream);
                stream.Seek(0, SeekOrigin.Begin);
                return true;
            }
            catch (Exception e)
            {
                throw new ExcelFormatError(e.Message, e);
            }
        }

        internal static byte[] SerializeInputExcel(this byte[] excelBytes, Methods method)
        {
            try
            {
                var jsonStream = new MemoryStream();
                using (var excelStream = new MemoryStream(excelBytes))
                using (var swr = new StreamWriter(jsonStream, UTF8, 1024, true))
                using (var jwr = new RESTarFromExcelJsonWriter(swr))
                using (var reader = ExcelReaderFactory.CreateOpenXmlReader(excelStream))
                {
                    jwr.WriteStartArray();
                    reader.Read();
                    var names = Range(0, reader.FieldCount)
                        .Select(i => reader[i].ToString())
                        .ToArray();
                    var objectCount = 0;
                    while (reader.Read())
                    {
                        jwr.WriteStartObject();
                        foreach (var i in Range(0, reader.FieldCount))
                        {
                            jwr.WritePropertyName(names[i]);
                            jwr.WriteValue(reader[i]);
                        }

                        jwr.WriteEndObject();
                        objectCount += 1;
                    }
                    if ((method == PATCH || method == PUT) && objectCount > 1)
                        throw new InvalidInputCount();
                    jwr.WriteEndArray();
                }
                return jsonStream.ToArray();
            }
            catch (Exception e)
            {
                throw new ExcelInputError(e.Message);
            }
        }

        #endregion

        internal static string GetString(this Stream stream)
        {
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        internal static string GetJsonUpdateString(this Stream stream)
        {
            string json;
            using (var reader = new StreamReader(stream))
                json = reader.ReadToEnd();
            if (json[0] == '[') throw new InvalidInputCount();
            return json;
        }

        internal static string GetJsonUpdateString(this byte[] bytes)
        {
            string json;
            using (var stream = new MemoryStream(bytes))
            using (var reader = new StreamReader(stream))
                json = reader.ReadToEnd();
            if (json[0] == '[') throw new InvalidInputCount();
            return json;
        }

        internal static string Serialize(this object value, Type type = null)
        {
            return JsonConvert.SerializeObject(value, type, _PrettyPrint ? Indented : None, Settings);
        }

        internal static byte[] SerializeToBytes(this object value, Type type = null)
        {
            return UTF8.GetBytes(JsonConvert.SerializeObject(value, type, _PrettyPrint ? Indented : None, Settings));
        }

        internal static IEnumerable<T> Populate<T>(this IEnumerable<T> source, string json)
        {
            T populated(T item)
            {
                Populate(json, item);
                return item;
            }

            foreach (var item in source)
                yield return populated(item);
        }

        internal static void Populate(string json, object target)
        {
            JsonConvert.PopulateObject(json, target, Settings);
        }

        internal static void Populate(JToken value, object target)
        {
            using (var sr = value.CreateReader())
                Json.Populate(sr, target);
        }

        internal static JToken ToJToken(this object o) => JToken.FromObject(o, Json);
        internal static T Deserialize<T>(this string json) => JsonConvert.DeserializeObject<T>(json);

        internal static List<T> DeserializeList<T>(this Stream jsonStream)
        {
            using (var streamReader = new StreamReader(jsonStream, UTF8))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                jsonReader.Read();
                if (jsonReader.TokenType == JsonToken.StartObject)
                    return new List<T> {Json.Deserialize<T>(jsonReader)};
                return Json.Deserialize<List<T>>(jsonReader);
            }
        }

        internal static List<T> DeserializeList<T>(this byte[] jsonBytes)
        {
            using (var jsonStream = new MemoryStream(jsonBytes))
            using (var streamReader = new StreamReader(jsonStream, UTF8))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                jsonReader.Read();
                if (jsonReader.TokenType == JsonToken.StartObject)
                    return new List<T> {Json.Deserialize<T>(jsonReader)};
                return Json.Deserialize<List<T>>(jsonReader);
            }
        }


        /// <summary>
        /// Deserializes the content of a stream to a given .NET object type
        /// </summary>
        public static T Deserialize<T>(this Stream jsonStream)
        {
            using (var streamReader = new StreamReader(jsonStream, UTF8))
            using (var jsonReader = new JsonTextReader(streamReader))
                return Json.Deserialize<T>(jsonReader);
        }

        /// <summary>
        /// Deserializes the content of a stream to a given .NET object type
        /// </summary>
        public static T Deserialize<T>(this byte[] jsonBytes)
        {
            using (var jsonStream = new MemoryStream(jsonBytes))
            using (var streamReader = new StreamReader(jsonStream, UTF8))
            using (var jsonReader = new JsonTextReader(streamReader))
                return Json.Deserialize<T>(jsonReader);
        }

        internal static string SerializeToViewModel(this object value)
        {
            return JsonConvert.SerializeObject(value, VmSettings);
        }
    }
}