using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RESTable.Resources.Operations;

namespace RESTable.Meta
{
    /// <inheritdoc />
    /// <summary>
    /// A dynamic property represents a dynamic property of a class, that is,
    /// a member that is not known at compile time.
    /// </summary>
    public class DynamicProperty : Property
    {
        /// <inheritdoc />
        public override bool IsDynamic => true;

        /// <summary>
        /// Should the evaluation fall back to a declared property if no dynamic property 
        /// can be found in the target entity?
        /// </summary>
        public readonly bool DeclaredFallback;

        /// <summary>
        /// Creates a dynamic property instance from a key string
        /// </summary>
        /// <param name="keyString">The string to parse</param>
        /// <param name="declaredFallback">Should the evaluation fall back to a declared property 
        /// if no dynamic property can be found in the target entity?</param>
        /// <returns>A dynamic property that represents the runtime property
        /// described by the key string</returns>
        public static DynamicProperty Parse(string keyString, bool declaredFallback = false) => new(keyString, declaredFallback);
        
        private DynamicProperty(string name, bool declaredFallback) : base(null)
        {
            Name = ActualName = name;
            DeclaredFallback = declaredFallback;
            Type = typeof(object);
            var typeCache = ApplicationServicesAccessor.TypeCache;

            Getter = obj =>
            {
                object? value;
                string? actualKey = null;

                ValueTask<object?> getFromStatic()
                {
                    var type = obj.GetType();
                    value = Do.Try(() =>
                    {
                        var prop = typeCache.FindDeclaredProperty(type, Name);
                        actualKey = prop?.Name;
                        return prop?.GetValue(obj);
                    }, default(object));
                    Name = actualKey ?? Name;
                    return new ValueTask<object?>(value);
                }

                switch (obj)
                {
                    case IDynamicMemberValueProvider dm:
                        if (dm.TryGetValue(Name, out value, out actualKey))
                        {
                            Name = actualKey;
                            return new ValueTask<object?>(value);
                        }
                        return DeclaredFallback ? getFromStatic() : new ValueTask<object?>(null);
                    case JObject jobj:
                        if (jobj.GetValue(Name, StringComparison.OrdinalIgnoreCase)?.Parent is not JProperty property)
                            return DeclaredFallback ? getFromStatic() : new ValueTask<object?>(null);
                        Name = property.Name;
                        return new ValueTask<object?>(property.Value.ToObject<object?>());
                    case IDictionary<string, object> dict:
                        string capitalized = Name.Capitalize();
                        if (dict.TryGetValue(capitalized, out value))
                        {
                            Name = capitalized;
                            return new ValueTask<object?>(null);
                        }
                        if (dict.TryFindInDictionary(Name, out actualKey, out value))
                        {
                            Name = actualKey!;
                            return new ValueTask<object?>(null);
                        }
                        return DeclaredFallback ? getFromStatic() : new ValueTask<object?>(null);
                    default: return getFromStatic();
                }
            };

            Setter = (obj, value) =>
            {
                switch (obj)
                {
                    case IDynamicMemberValueProvider dm:
                        dm.TrySetValue(Name, value);
                        break;
                    case IDictionary<string, object?> ddict:
                        ddict[Name] = value;
                        break;
                    case IDictionary idict:
                        idict[Name] = value;
                        break;
                    case IDictionary<string, JToken?> jobj:
                        jobj[Name] = (dynamic?) value;
                        break;
                    default:
                        var type = obj.GetType();
                        Do.Try(() => typeCache.FindDeclaredProperty(type, Name)?.SetValue(obj, value));
                        break;
                }
                return default;
            };
        }
    }
}