using System;
using System.Collections.Generic;
using System.Reflection;
using Dynamit;
using Starcounter;

namespace RESTar.Internal
{
    public abstract class Property
    {
        public virtual string Name { get; set; }
        public virtual string DatabaseQueryName { get; set; }
        public abstract bool Dynamic { get; }
        public bool Static => !Dynamic;
        public abstract bool IsStarcounterQueryable { get; }

        public static Property Parse(string keyString, Type resource, bool dynamic)
        {
            if (dynamic) return DynamicProperty.Parse(keyString);
            return StaticProperty.Parse(keyString, resource);
        }

        internal abstract dynamic GetValue(dynamic root);
    }
}