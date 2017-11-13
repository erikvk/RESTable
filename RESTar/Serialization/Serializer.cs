using System.Collections.Generic;
using System.IO;
using System.Xml;
using System;
using System.Data;
using System.Linq;
using System.Text;
using ExcelDataReader;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
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
        internal static readonly JsonSerializerSettings Settings;
        internal static readonly JsonSerializer JsonSerializer;

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
            Settings.Converters.Add(enumConverter);
            VmSettings.Converters.Add(enumConverter);
            JsonSerializer = JsonSerializer.Create(Settings);
        }

        internal static bool GetJsonStream(this object data, out MemoryStream stream, out long count)
        {
            JsonSerializer.Formatting = _PrettyPrint ? Indented : None;
            stream = new MemoryStream();
            using (var swr = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            using (var jwr = new RESTarJsonWriter(swr))
            {
                JsonSerializer.Serialize(jwr, data);
                jwr.Flush();
                swr.Flush();
                count = jwr.ObjectsWritten;
            }
            if (count == 0) return false;
            stream.Seek(0, SeekOrigin.Begin);
            return true;
        }

        internal static bool GetExcelStream(this IEnumerable<object> data, IResource resource, out MemoryStream stream,
            out long count)
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
                throw new ExcelFormatException(e.Message, e);
            }
        }

        private static readonly ExcelDataSetConfiguration excelDataSetConfig = new ExcelDataSetConfiguration
        {
            ConfigureDataTable = s => new ExcelDataTableConfiguration {UseHeaderRow = true},
            UseColumnDataType = true
        };

        internal static DataSet GetDataSet(this IExcelDataReader reader)
        {
            return reader.AsDataSet(excelDataSetConfig) ?? throw new ExcelInputException();
        }

        internal static void GetJsonStreamFromExcel(this DataTable table, out MemoryStream stream)
        {
            stream = new MemoryStream();
            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            using (var jsonWriter = new RESTarFromExcelJsonWriter(streamWriter))
            {
                JsonSerializer.Serialize(jsonWriter, table);
                jsonWriter.Flush();
                streamWriter.Flush();
            }
            stream.Seek(0, SeekOrigin.Begin);
        }

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
            if (json[0] == '[') throw new InvalidInputCountException();
            return json;
        }

        internal static string Serialize(this object value, Type type = null)
        {
            return JsonConvert.SerializeObject(value, type, _PrettyPrint ? Indented : None, Settings);
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
                JsonSerializer.Populate(sr, target);
        }

        internal static JToken ToJToken(this object o) => JToken.FromObject(o, JsonSerializer);
        internal static dynamic Deserialize(this string json, Type type) => JsonConvert.DeserializeObject(json, type);
        internal static JToken Deserialize(this string json) => JsonConvert.DeserializeObject<JToken>(json);
        internal static T Deserialize<T>(this string json) => JsonConvert.DeserializeObject<T>(json);

        internal static List<T> DeserializeList<T>(this Stream jsonStream)
        {
            using (var jsonReader = new JsonTextReader(new StreamReader(jsonStream)))
            {
                jsonReader.Read();
                if (jsonReader.TokenType == JsonToken.StartObject)
                    return new List<T> {JsonSerializer.Deserialize<T>(jsonReader)};
                return JsonSerializer.Deserialize<List<T>>(jsonReader);
            }
        }

        internal static T Deserialize<T>(this Stream jsonStream)
        {
            using (var jsonReader = new JsonTextReader(new StreamReader(jsonStream)))
                return JsonSerializer.Deserialize<T>(jsonReader);
        }

        internal static string SerializeToViewModel(this object value) => JsonConvert.SerializeObject(value, VmSettings);
    }
}