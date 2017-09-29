using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RESTar.Operations;

namespace RESTar.Deflection.Dynamic
{
    /// <inheritdoc />
    /// <summary>
    /// A dynamic property represents a dynamic property of a class, that is,
    /// a member that is not known at compile time.
    /// </summary>
    public class DynamicProperty : Property
    {
        /// <inheritdoc />
        public override bool Dynamic => true;

        /// <summary>
        /// Creates a dynamic property instance from a key string
        /// </summary>
        /// <param name="keyString">The string to parse</param>
        /// <returns>A dynamic property that represents the runtime property
        /// described by the key string</returns>
        public static DynamicProperty Parse(string keyString) => new DynamicProperty(keyString);

        internal void SetName(string name) => Name = name;

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
                        if (!ddict.TryGetNoCase(Name, out actualKey, out value))
                            return null;
                        Name = actualKey;
                        return value;
                    case IDictionary idict:
                        if (!idict.TryGetNoCase(Name, out actualKey, out value))
                            return null;
                        Name = actualKey;
                        return value;
                    case IDictionary<string, JToken> jobj:
                        if (!jobj.TryGetNoCase(Name, out actualKey, out var jvalue))
                            return null;
                        Name = actualKey;
                        return jvalue.ToObject<dynamic>();
                    default:
                        var type = obj.GetType();
                        value = Do.Try(() =>
                        {
                            var prop = StaticProperty.Find(type, Name);
                            actualKey = prop.Name;
                            return prop.GetValue(obj);
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
                Do.Try(() => StaticProperty.Find(type, Name)?.SetValue(obj, value));
            };
        }
    }
}