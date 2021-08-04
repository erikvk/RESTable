using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RESTable.Meta;

namespace RESTable.Json
{
    public class DynamicMemberPopulatorCacheEqualityComparer : IEqualityComparer<(string, Type)>
    {
        public static readonly IEqualityComparer<(string, Type)> Instance = new DynamicMemberPopulatorCacheEqualityComparer();
        public bool Equals((string, Type) x, (string, Type) y) => x.Item2 == y.Item2 && string.Equals(x.Item1, y.Item1, StringComparison.OrdinalIgnoreCase);
        public int GetHashCode((string, Type) obj) => (obj.Item1.ToLowerInvariant(), obj.Item2).GetHashCode();
    }

    public class DynamicMemberPopulatorCache : Dictionary<(string key, Type type), Populator>
    {
        public DynamicMemberPopulatorCache() : base(DynamicMemberPopulatorCacheEqualityComparer.Instance) { }
    }

    public readonly struct Populator
    {
        private delegate ValueTask<object> PopulatorAction(object target);

        private PopulatorAction Action { get; }

        public Populator(Type toPopulate, JsonElement jsonElement, TypeCache typeCache, JsonSerializerOptions options)
        {
            if (!toPopulate.CanBePopulated())
                throw new InvalidOperationException($"Cannot populate onto type '{toPopulate.GetRESTableTypeName()}'");

            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Null:
                {
                    Action = _ => default;
                    return;
                }
                case not JsonValueKind.Object: throw new InvalidOperationException($"Cannot populate from element of kind '{jsonElement.ValueKind}'. Expected Object");
            }

            var (populateDeclaredProperties, otherProperties) = GetPopulateDeclaredPropertiesAction(toPopulate, jsonElement, typeCache, options);
            DynamicMemberPopulatorCache? dynamicCache = null;

            async ValueTask<object> populate(object target)
            {
                await populateDeclaredProperties(target).ConfigureAwait(false);

                if (otherProperties.Count == 0 || target is not IDictionary<string, object?> dict)
                    return target;

                dynamicCache ??= new DynamicMemberPopulatorCache();

                for (var index = 0; index < otherProperties.Count; index += 1)
                {
                    var property = otherProperties[index];
                    if (dict.TryGetValue(property.Name, out var existingValue) && existingValue is not null)
                    {
                        dict[property.Name] = await GetDynamicValue(property.Name, existingValue, property.Value, dynamicCache, typeCache, options).ConfigureAwait(false);
                    }
                    else dict[property.Name] = property.Value.ToObject<object>(options);
                }
                return target;
            }

            Action = populate;
        }

        private static (PopulatorAction action, List<JsonProperty> otherProperties) GetPopulateDeclaredPropertiesAction
        (
            Type toPopulate,
            JsonElement newValueElement,
            TypeCache typeCache,
            JsonSerializerOptions options
        )
        {
            var declaredProperties = typeCache.GetDeclaredProperties(toPopulate);
            var jsonProperties = newValueElement.EnumerateObject().ToList();
            var actions = new PopulatorAction[jsonProperties.Count];
            var actionCount = 0;

            for (var index = 0; index < jsonProperties.Count; index += 1)
            {
                var jsonProperty = jsonProperties[index];
                if (!declaredProperties.TryGetValue(jsonProperty.Name, out var declaredProperty))
                {
                    // We have encountered an unknown member in the input JSON. Let's skip it.
                    continue;
                }
                jsonProperties.RemoveAt(index);
                index -= 1;
                actions[actionCount] = GetDeclaredPropertyAction(declaredProperty!, jsonProperty.Value, typeCache, options);
                actionCount += 1;
            }

            async ValueTask<object> populateAction(object parent)
            {
                for (var actionIndex = 0; actionIndex < actionCount; actionIndex += 1)
                {
                    await actions[actionIndex].Invoke(parent).ConfigureAwait(false);
                }
                return parent;
            }

            return (populateAction, jsonProperties);
        }

        private static async ValueTask<object> GetDynamicValue
        (
            string propertyName,
            object existingValue,
            JsonElement propertyValue,
            DynamicMemberPopulatorCache dynamicMemberPopulatorCache,
            TypeCache typeCache,
            JsonSerializerOptions options
        )
        {
            var existingType = existingValue.GetType();
            if (!existingType.CanBePopulated())
            {
                // The existing value cannot be populated, but we know what type is expected.
                return propertyValue.ToObject(existingType, options)!;
            }
            // There is an existing value, and it can be populated. Let's check if we have encountered this property before.
            // if we have, we do not need to construct a new populator for it.
            if (!dynamicMemberPopulatorCache.TryGetValue((propertyName, existingType), out var populator))
                populator = dynamicMemberPopulatorCache[(propertyName, existingType)] = new Populator(existingType, propertyValue, typeCache, options);
            var populatedValue = await populator.PopulateAsync(existingValue).ConfigureAwait(false);
            return populatedValue;
        }

        private static PopulatorAction GetDeclaredPropertyAction(DeclaredProperty declaredProperty, JsonElement jsonValue, TypeCache typeCache, JsonSerializerOptions options)
        {
            // If it's writable and should not be populated, make a set value action for it
            if (declaredProperty!.IsWritable && (!declaredProperty.CanBePopulated || declaredProperty.ReplaceOnUpdate))
            {
                var hasPresetValue = false;
                // If the value is null or a value type, we can assign the same (possibly boxed) value to all properties.
                // that value can be established right now, and stored in presetValue.
                object? presetValue = null;
                if (jsonValue.ValueKind == JsonValueKind.Null)
                {
                    hasPresetValue = true;
                }
                else if (declaredProperty.IsValueType)
                {
                    presetValue = jsonValue.ToObject(declaredProperty.Type, options);
                    hasPresetValue = true;
                }

                // Return a set value action for this property
                async ValueTask<object> setValueAction(object parent)
                {
                    var value = hasPresetValue ? presetValue : jsonValue.ToObject(declaredProperty.Type, options);
                    await declaredProperty.SetValue(parent, value).ConfigureAwait(false);
                    return parent;
                }

                return setValueAction;
            }

            if (declaredProperty.Type == typeof(object))
            {
                // If the property is of type object, we expect a dynamically populated value based on the runtime 
                // type of the property.

                DynamicMemberPopulatorCache? dynamicCache = null;

                async ValueTask<object> dynamicPopulateAction(object parent)
                {
                    var existingValue = await declaredProperty.GetValue(parent).ConfigureAwait(false);
                    if (existingValue is null)
                    {
                        if (!declaredProperty.IsWritable)
                        {
                            // Not much we can do, the value was null and we can't change it.
                            return parent;
                        }
                        // Nothing to populate, so we create a new value instead.
                        var value = jsonValue.ToObject(declaredProperty.Type, options);
                        await declaredProperty.SetValue(parent, value).ConfigureAwait(false);
                    }
                    else
                    {
                        dynamicCache ??= new DynamicMemberPopulatorCache();
                        // There is an existing non-null value, and it can be populated. Let's do it.
                        var value = await GetDynamicValue(declaredProperty.Name, existingValue, jsonValue, dynamicCache, typeCache, options).ConfigureAwait(false);
                        if (declaredProperty.IsWritable) await declaredProperty.SetValue(parent, value).ConfigureAwait(false);
                    }
                    return parent;
                }

                return dynamicPopulateAction;
            }

            // It is a statically known type, and should be populated (we don't know if it's writable). Make a populator for it.
            var populator = new Populator(declaredProperty.Type, jsonValue, typeCache, options);

            async ValueTask<object> staticPopulateAction(object parent)
            {
                var existingValue = await declaredProperty.GetValue(parent).ConfigureAwait(false);
                if (existingValue is null)
                {
                    if (!declaredProperty.IsWritable)
                    {
                        // Not much we can do, the value was null and we can't change it.
                        return parent;
                    }
                    // Nothing to populate, so we create a new value instead.
                    var value = jsonValue.ToObject(declaredProperty.Type, options);
                    await declaredProperty.SetValue(parent, value).ConfigureAwait(false);
                }
                else
                {
                    // There is an existing non-null value, and it can be populated. Let's do it.
                    var populatedValue = await populator.PopulateAsync(existingValue).ConfigureAwait(false);
                    if (declaredProperty.IsWritable) await declaredProperty.SetValue(parent, populatedValue).ConfigureAwait(false);
                }
                return parent;
            }

            return staticPopulateAction;
        }


        /// <summary>
        /// Takes a target object and populate it, returning a populated version
        /// </summary>
        public ValueTask<object> PopulateAsync(object target) => Action(target);
    }
}