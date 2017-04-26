using System;
using System.Collections.Generic;
using System.Reflection;
using Dynamit;

namespace RESTar.Internal
{
    public class DynamicProperty : Property
    {
        public override bool Dynamic => true;
        public override bool IsStarcounterQueryable => false;

        public DynamicProperty(string name)
        {
            Name = DatabaseQueryName = name;
        }

        public static DynamicProperty Parse(string keyString)
        {
            return new DynamicProperty(keyString);
        }

        internal override dynamic GetValue(dynamic input)
        {
            var ddict = input as IDictionary<string,dynamic>;
            if (ddict != null) return ddict.SafeGetNoCase(Name);
            Type type = input.GetType();
            return type.FindProperty(Name)?.GetValue(input);
        }
    }
}