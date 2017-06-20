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
                switch (obj)
                {
                    case IDictionary<string, dynamic> ddict: return ddict.SafeGetNoCase(Name);
                    case JObject jobj: return jobj.SafeGetNoCase(Name)?.ToObject<dynamic>();
                    default:
                        var type = obj.GetType();
                        return Do.Try(() => type.MatchProperty(Name)?.Get(obj), null);
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