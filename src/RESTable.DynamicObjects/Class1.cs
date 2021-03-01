using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RESTable.DynamicObjects
{
    public static class DynamicObjectExtensionMethods
    {
        /// <summary>
        /// Converts a Dictionary object to a JSON.net JObject
        /// </summary>
        public static JObject ToJObject(this Dictionary<string, dynamic> d)
        {
            var jobj = new JObject();
            foreach (var (key, value) in d)
            {
                jobj[key] = MakeJToken(value);
            }
            return jobj;
        }

        private static JToken MakeJToken(dynamic value)
        {
            try
            {
                return (JToken) value;
            }
            catch
            {
                try
                {
                    return new JArray(value);
                }
                catch
                {
                    return JToken.FromObject(value);
                }
            }
        }


    }
}