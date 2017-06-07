using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Starcounter;

namespace RESTar.Deflection
{
    public class StaticProperty : Property
    {
        public override string Name { get; protected set; }
        public override string DatabaseQueryName { get; protected set; }
        public Type Type { get; protected set; }
        public override bool Dynamic => false;
        public override bool ScQueryable { get; protected set; }
        public IEnumerable<Attribute> Attributes;

        internal TAttribute GetAttribute<TAttribute>() where TAttribute : Attribute => Attributes
            .OfType<TAttribute>().FirstOrDefault();

        internal bool HasAttribute<TAttribute>() where TAttribute : Attribute => GetAttribute<TAttribute>() != null;

        internal StaticProperty(PropertyInfo p)
        {
            if (p == null) return;
            Name = p.RESTarMemberName();
            DatabaseQueryName = p.Name;
            Type = p.PropertyType;
            ScQueryable = p.DeclaringType?.HasAttribute<DatabaseAttribute>() == true &&
                          p.PropertyType.IsStarcounterCompatible();
            Attributes = p.GetCustomAttributes();
            Getter = p.MakeDynamicGetter();
            Setter = p.MakeDynamicSetter();
        }

        protected StaticProperty(bool scQueryable) => ScQueryable = scQueryable;

        public static StaticProperty Parse(string keyString, Type resource) => resource.MatchProperty(keyString);
    }
}