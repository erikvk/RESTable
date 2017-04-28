using System;
using System.Collections.Generic;
using static RESTar.Operations.Do;

namespace RESTar.Internal
{
    public class DynamicProperty : Property
    {
        public override bool Dynamic => true;
        public override bool ScQueryable => false;
        public static DynamicProperty Parse(string keyString) => new DynamicProperty(keyString);

        internal DynamicProperty(string name)
        {
            Name = DatabaseQueryName = name;
        }

        internal override dynamic GetValue(dynamic input)
        {
            var ddict = input as IDictionary<string, dynamic>;
            if (ddict != null) return ddict.SafeGetNoCase(Name);
            Type type = input.GetType();
            return Try(() => type.MatchProperty(Name)?.GetValue(input), null);
        }
    }
}