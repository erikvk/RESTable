using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RESTar.Operations;

namespace RESTar.Deflection
{
    public class DynamicProperty : Property
    {
        public override bool Dynamic => true;
        public override bool ScQueryable { get; protected set; }

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
                            var prop = type.MatchProperty(Name);
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
                Do.Try(() => type.MatchProperty(Name)?.Set(obj, value));
            };
        }
    }
}