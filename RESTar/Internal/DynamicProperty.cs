using System.Collections.Generic;
using static RESTar.Operations.Do;

namespace RESTar.Internal
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
                if (obj is IDictionary<string, dynamic> ddict)
                    return ddict.SafeGetNoCase(Name);
                var type = obj.GetType();
                return Try(() => type.MatchProperty(Name)?.Get(obj), null);
            };

            Setter = (obj, value) =>
            {
                if (obj is IDictionary<string, dynamic> ddict)
                    ddict[Name] = value;
                var type = obj.GetType();
                Try(() => type.MatchProperty(Name)?.Set(obj, value));
            };
        }
    }
}