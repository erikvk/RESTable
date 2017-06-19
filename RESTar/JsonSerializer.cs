﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Dynamit;
using Jil;
using Newtonsoft.Json;
using RESTar.View.Serializer;
using Starcounter;
using static Jil.DateTimeFormat;
using static Jil.UnspecifiedDateTimeKindBehavior;
using static Newtonsoft.Json.Formatting;
using static RESTar.Settings;

namespace RESTar
{
    internal static class JsonSerializer
    {
        static JsonSerializer()
        {
            SerializeMethod = typeof(JSON).GetMethods()
                .First(n => n.Name == "Serialize" && n.ReturnType == typeof(void));
            VmSettings = new JsonSerializerSettings
            {
                ContractResolver = new CreateViewModelResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };
            VmSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        }

        internal static Options SerializerOptions { private get; set; }

        internal static JsonSerializerSettings JsonNetSettings;

        private static readonly MethodInfo SerializeMethod;

        private static Options VmSerializerOptions => new Options(excludeNulls: true, includeInherited: true,
            dateFormat: ISO8601, unspecifiedDateTimeKindBehavior: _LocalTimes ? IsLocal : IsUTC);

        internal static string Serialize(this object obj, Type resource)
        {
            var generic = SerializeMethod.MakeGenericMethod(resource);
            var writer = new StringWriter();
            try
            {
                generic.Invoke(null, new[] {obj, writer, SerializerOptions});
            }
            catch (DbException)
            {
            }
            return writer.ToString();
        }

        internal static string SerializeVmJsonTemplate(this IDictionary<string, dynamic> template)
        {
            return JSON.SerializeDynamic(template, VmSerializerOptions);
        }

        internal static string SerializeDynamicResourceToViewModel(this IDictionary<string, dynamic> template)
        {
            return JSON.SerializeDynamic(template, VmSerializerOptions);
        }

        internal static string SerializeDyn<T>(this IEnumerable<T> obj)
        {
            var type = obj.FirstOrDefault()?.GetType();
            if (typeof(IDictionary<string, object>).IsAssignableFrom(type))
                return JsonNetSerialize(obj);
            return JSON.SerializeDynamic(obj, SerializerOptions);
        }

        internal static string SerializeDyn(this object obj)
        {
            return JSON.SerializeDynamic(obj, SerializerOptions);
        }

        internal static dynamic DeserializeDyn(this string json)
        {
            return JSON.DeserializeDynamic(json, SerializerOptions);
        }

        internal static dynamic Deserialize(this string json, Type resource)
        {
            return JSON.Deserialize(json, resource, SerializerOptions);
        }

        internal static dynamic Deserialize<T>(this string json)
        {
            return JSON.Deserialize<T>(json, SerializerOptions);
        }

        internal static void PopulateObject(string json, object target)
        {
            JsonConvert.PopulateObject(json, target, JsonNetSettings);
        }

        internal static string JsonNetSerialize(this object value)
        {
            return _PrettyPrint
                ? JsonConvert.SerializeObject(value, Indented, JsonNetSettings).Replace("\r\n", "\n")
                : JsonConvert.SerializeObject(value, None, JsonNetSettings);
        }

        private static readonly JsonSerializerSettings VmSettings;

        internal static string SerializeStaticResourceToViewModel(this object value)
        {
            return JsonConvert.SerializeObject(value, VmSettings);
        }
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