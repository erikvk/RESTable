using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dynamit;
using Starcounter;
using static RESTar.RESTarConfig;

namespace RESTar.Internal
{
    public class PropertyChain : List<Property>
    {
        public string Key => string.Join(".", this.Select(p => p.Name));
        public string DbKey => string.Join(".", this.Select(p => p.DatabaseQueryName));
        public bool IsStarcounterQueryable => this.All(p => p.IsStarcounterQueryable);

        internal static PropertyChain Parse(string keyString, IResource resource)
        {
            var chain = new PropertyChain();
            var propertyMaker = new Func<string, Property>(str =>
            {
                if (string.IsNullOrWhiteSpace(str))
                    throw new SyntaxException($"Invalid condition '{str}'",
                        ErrorCode.InvalidConditionSyntaxError);
                if (str == "objectno") return StaticProperty.ObjectNo;
                if (str == "objectid") return StaticProperty.ObjectID;
                var previous = chain.LastOrDefault();
                if (previous == null)
                    return Property.Parse(str, resource.TargetType, resource.IsDynamic);
                if (previous.Static)
                {
                    var _previous = (StaticProperty) previous;
                    if (typeof(DDictionary).IsAssignableFrom(_previous.Type))
                        return DynamicProperty.Parse(str);
                    return StaticProperty.Parse(str, ((StaticProperty) previous).Type);
                }
                return DynamicProperty.Parse(str);
            });
            keyString.ToLower().Split('.').ForEach(s => chain.Add(propertyMaker(s)));
            return chain;
        }

        internal static PropertyChain ParseDynamic(string keyString)
        {
            var chain = new PropertyChain();
            var propertyMaker = new Func<string, Property>(str =>
            {
                if (string.IsNullOrWhiteSpace(str))
                    throw new SyntaxException($"Invalid condition '{str}'",
                        ErrorCode.InvalidConditionSyntaxError);
                if (str == "objectno") return StaticProperty.ObjectNo;
                if (str == "objectid") return StaticProperty.ObjectID;
                return DynamicProperty.Parse(str);
            });
            keyString.ToLower().Split('.').ForEach(s => chain.Add(propertyMaker(s)));
            return chain;
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

        internal dynamic GetValue(dynamic val)
        {
            foreach (var prop in this)
            {
                if (val == null) return null;
                val = prop.GetValue(val);
            }
            return val;
        }
    }
}