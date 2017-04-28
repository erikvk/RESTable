using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Dynamit;
using Jil;
using Newtonsoft.Json;
using Starcounter;

namespace RESTar
{
    internal static class JsonSerializer
    {
        private static Options _serializerOptions;

        internal static Options SerializerOptions
        {
            get { return _serializerOptions; }
            set
            {
                if (!value.Equals(_serializerOptions))
                {
                    _serializerOptions = value;
                    InitSerializers();
                }
                _serializerOptions = value;
            }
        }

        internal static JsonSerializerSettings JsonNetSettings;

        internal static string SerializeDyn<T>(this IEnumerable<T> obj)
        {
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

        private static readonly MethodInfo SerializeMethod =
            typeof(JSON).GetMethods().First(n => n.Name == "Serialize" && n.ReturnType == typeof(void));

        internal static string Serialize(this object obj, Type resource)
        {
            var generic = SerializeMethod.MakeGenericMethod(resource);
            var writer = new StringWriter();
            generic.Invoke
            (
                null,
                new[]
                {
                    obj,
                    writer,
                    SerializerOptions
                }
            );
            return writer.ToString();
        }

        internal static void InitSerializer(Type resource)
        {
            if (resource.IsAbstract || resource.IsSubclassOf(typeof(DDictionary))) return;
            Scheduling.ScheduleTask(() =>
            {
                try
                {
                    JSON.Deserialize("{}", resource, SerializerOptions);
                }
                catch (Exception)
                {
                }
            });
        }

        internal static void InitSerializers()
        {
            foreach (var resource in RESTarConfig.Resources.Where(
                r => !r.TargetType.IsAbstract && !r.TargetType.IsSubclassOf(typeof(DDictionary)))
            )
            {
                Scheduling.ScheduleTask(() =>
                {
                    try
                    {
                        JSON.Deserialize("{}", resource.TargetType, SerializerOptions);
                    }
                    catch (Exception)
                    {
                    }
                });
            }
        }

        internal static void PopulateObject(string json, object target, JsonSerializerSettings settings = null)
        {
            JsonConvert.PopulateObject(json, target, settings ?? JsonNetSettings);
        }

        internal static string JsonNetSerialize(this object value)
        {
            return JsonConvert.SerializeObject(value, JsonNetSettings);
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
        internal static DataSet ToDataSet(this IEnumerable<dynamic> list)
        {
            var ds = new DataSet();
            var t = new DataTable();
            ds.Tables.Add(t);

            var first = list.First();
            if (first is IDictionary<string, dynamic>)
            {
                foreach (var item in list)
                {
                    var row = t.NewRow();
                    foreach (var pair in item)
                    {
                        try
                        {
                            if (!t.Columns.Contains(pair.Key))
                                t.Columns.Add(pair.Key);
                            row[pair.Key] = pair.Value ?? DBNull.Value;
                        }
                        catch
                        {
                            try
                            {
                                row[pair.Key] = DbHelper.GetObjectNo(pair.Value) ?? DBNull.Value;
                            }
                            catch
                            {
                                row[pair.Key] = pair.Value?.ToString() ?? DBNull.Value;
                            }
                        }
                    }
                    t.Rows.Add(row);
                }
            }
            else
            {
                Type elementType = first.GetType();
                foreach (var propInfo in elementType.GetPropertyList())
                {
                    var ColType = propInfo.PropertyType.IsClass && propInfo.PropertyType != typeof(string)
                        ? typeof(string)
                        : Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;
                    t.Columns.Add(propInfo.Name, ColType);
                }
                foreach (var item in list)
                {
                    var row = t.NewRow();
                    foreach (var propInfo in elementType.GetPropertyList())
                    {
                        var value = propInfo.GetValue(item, null);
                        try
                        {
                            row[propInfo.Name] = propInfo.HasAttribute<ExcelFlattenToString>()
                                ? value.ToString()
                                : "$(ObjectID: " + DbHelper.GetObjectID(value) + ")";
                        }
                        catch
                        {
                            row[propInfo.Name] = value ?? DBNull.Value;
                        }
                    }
                    t.Rows.Add(row);
                }
            }
            return ds;
        }
    }
}