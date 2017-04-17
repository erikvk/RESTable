using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Dynamit;
using Jil;
using Newtonsoft.Json;
using Starcounter;

namespace RESTar
{
    internal static class Serializer
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

        internal static void PopulateObject(string json, object target)
        {
            JsonConvert.PopulateObject(json, target, JsonNetSettings);
        }

        internal static string JsonNetSerialize(this object value)
        {
            return JsonConvert.SerializeObject(value, JsonNetSettings);
        }
    }
}