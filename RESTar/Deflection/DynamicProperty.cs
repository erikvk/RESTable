using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RESTar.Operations;

namespace RESTar.Deflection
{
    /// <summary>
    /// A dynamic property represents a dynamic property of a class, that is,
    /// a member that is not known at compile time.
    /// </summary>
    public class DynamicProperty : Property
    {
        /// <summary>
        /// Is this property dynamic?
        /// </summary>
        public override bool Dynamic => true;

        /// <summary>
        /// Creates a dynamic property instance from a key string
        /// </summary>
        /// <param name="keyString">The string to parse</param>
        /// <returns>A dynamic property that represents the runtime property
        /// described by the key string</returns>
        public static DynamicProperty Parse(string keyString) => new DynamicProperty(keyString);

        internal DynamicProperty(string name)
        {
            Name = DatabaseQueryName = name;
            ScQueryable = false;

            Getter = obj =>
            {
                dynamic value;
                string actualKey = null;
                switch (obj)
                {
                    case IDictionary<string, dynamic> ddict:
                        value = ddict.SafeGetNoCase(Name, out actualKey);
                        Name = actualKey ?? Name;
                        return value;
                    case IDictionary idict:
                        value = idict.SafeGetNoCase(Name, out actualKey);
                        Name = actualKey ?? Name;
                        return value;
                    case JObject jobj:
                        value = jobj.SafeGetNoCase(Name, out actualKey)?.ToObject<dynamic>();
                        Name = actualKey ?? Name;
                        return value;
                    default:
                        var type = obj.GetType();
                        value = Do.Try(() =>
                        {
                            var prop = StaticProperty.Get(type, Name);
                            actualKey = prop.Name;
                            return prop.Get(obj);
                        }, default(object));
                        Name = actualKey ?? Name;
                        return value;
                }
            };

            Setter = (obj, value) =>
            {
                if (obj is IDictionary<string, dynamic> ddict)
                    ddict[Name] = value;
                if (obj is JObject jobj)
                    jobj[Name] = value;
                var type = obj.GetType();
                Do.Try(() => StaticProperty.Get(type, Name)?.Set(obj, value));
            };
        }
    }
}