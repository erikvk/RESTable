using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Dynamit;
using Jil;
using Newtonsoft.Json;
using Starcounter;

namespace RESTar
{
    public static class Serializer
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

        internal static string SerializeDyn(this object obj)
        {
            return JSON.SerializeDynamic(obj, SerializerOptions);
        }

        internal static dynamic Deserialize(this string json, Type resource)
        {
            return JSON.Deserialize(json, resource, SerializerOptions);
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

        internal static void InitSerializers()
        {
            foreach (var resource in RESTarConfig.ResourcesList.Where(r => !r.IsSubclassOf(typeof(DDictionary))))
            {
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
        }

        internal static void PopulateObject(string json, object target)
        {
            JsonConvert.PopulateObject(json, target, JsonNetSettings);
        }

        internal static string JsonNetSerialize(object value)
        {
            return JsonConvert.SerializeObject(value, JsonNetSettings);
        }
    }
}