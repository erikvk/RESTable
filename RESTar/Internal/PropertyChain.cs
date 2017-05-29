using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using static RESTar.ErrorCode;

namespace RESTar.Internal
{
    public class PropertyChain : List<Property>
    {
        public string Key => string.Join(".", this.Select(p => p.Name));
        public string DbKey => string.Join(".", this.Select(p => p.DatabaseQueryName));
        public bool ScQueryable => this.All(p => p.ScQueryable);
        private static readonly NoCaseComparer Comparer = new NoCaseComparer();

        internal static PropertyChain Parse(string keyString, IResource resource, List<string> dynamicDomain = null)
        {
            var chain = new PropertyChain();

            Property propertyMaker(string str)
            {
                if (string.IsNullOrWhiteSpace(str))
                    throw new SyntaxException(InvalidConditionSyntaxError, $"Invalid condition '{str}'");
                if (dynamicDomain?.Contains(str, Comparer) == true)
                    return DynamicProperty.Parse(str);
                var previous = chain.LastOrDefault();
                if (previous == null)
                    return Property.Parse(str, resource.TargetType, resource.IsDDictionary);
                if (previous is StaticProperty _static)
                {
                    if (_static.Type.IsSubclassOf(typeof(DDictionary)))
                        return DynamicProperty.Parse(str);
                    return StaticProperty.Parse(str, _static.Type);
                }
                return DynamicProperty.Parse(str);
            }

            keyString.Split('.').ForEach(s => chain.Add(propertyMaker(s)));
            return chain;
        }

        internal void MakeDynamic()
        {
            var newProperties = this.Select(prop =>
                {
                    if (prop is StaticProperty stat && !(stat is SpecialProperty))
                        return new DynamicProperty(prop.Name);
                    return prop;
                })
                .ToList();
            Clear();
            AddRange(newProperties);
        }

        internal void Migrate(Type type)
        {
            StaticProperty previousStatic = null;
            foreach (var property in this)
            {
                if (property.Dynamic) return;
                var stat = (StaticProperty) property;
                stat.Migrate(type, previousStatic);
                previousStatic = stat;
            }
        }

        internal dynamic Get(object obj)
        {
            if (obj is IDictionary<string, dynamic>)
                MakeDynamic();
            foreach (var prop in this)
            {
                if (obj == null) return null;
                obj = prop.Get(obj);
            }
            return obj;
        }
    }
}