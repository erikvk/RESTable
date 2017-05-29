using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Starcounter;
using Deflector.Dynamic;
using RESTar.Operations;

namespace RESTar.Internal
{
    public class SpecialProperty : StaticProperty
    {
        public SpecialProperty(bool scQueryable) : base(scQueryable)
        {
        }

        public static SpecialProperty ObjectNo => new SpecialProperty(true)
        {
            Name = "ObjectNo",
            DatabaseQueryName = "ObjectNo",
            Type = typeof(ulong),
            Getter = t => Do.TryAndThrow(() => t.GetObjectNo(),
                "Could not get ObjectNo from non-Starcounter resource."),
            Attributes = new[] {new UniqueId()}
        };

        public static SpecialProperty ObjectID => new SpecialProperty(true)
        {
            Name = "ObjectID",
            DatabaseQueryName = "ObjectID",
            Type = typeof(string),
            Getter = t => Do.TryAndThrow(() => t.GetObjectID(),
                "Could not get ObjectID from non-Starcounter resource."),
            Attributes = new[] {new UniqueId()}
        };
    }

    public class StaticProperty : Property
    {
        public override string Name { get; protected set; }
        public override string DatabaseQueryName { get; protected set; }
        public Type Type { get; protected set; }
        public override bool Dynamic => false;
        public override bool ScQueryable { get; protected set; }
        public IEnumerable<Attribute> Attributes;

        internal TAttribute GetAttribute<TAttribute>() where TAttribute : Attribute =>
            Attributes.OfType<TAttribute>().FirstOrDefault();

        internal bool HasAttribute<TAttribute>() where TAttribute : Attribute =>
            GetAttribute<TAttribute>() != null;

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

        protected StaticProperty(bool scQueryable)
        {
            ScQueryable = scQueryable;
        }

        private void Populate(StaticProperty property)
        {
            Name = property.Name;
            DatabaseQueryName = property.DatabaseQueryName;
            Type = property.Type;
            ScQueryable = property.ScQueryable;
            Attributes = property.Attributes;
            Getter = property.Getter;
            Setter = property.Setter;
        }

        public static StaticProperty Parse(string keyString, Type resource) => resource.MatchProperty(keyString);

        internal void Migrate(Type type, StaticProperty previous)
        {
            if (this is SpecialProperty) return;
            var parentType = previous?.Type ?? type;
            var property = parentType.MatchProperty(Name);
            Populate(property);
        }
    }
}