using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RESTable.Meta;

namespace RESTable
{
    public readonly struct Populator
    {
        private static PopulatorAction DoNothing => t => new ValueTask<object>(t);
        private static PopulatorAction SetToNull => _ => default;

        private PopulatorAction Action { get; }

        public Populator(Type toPopulate, PopulateSource source, TypeCache typeCache)
        {
            if (!typeCache.CanBePopulated(toPopulate))
                throw new InvalidOperationException($"Cannot populate onto type '{toPopulate.GetRESTableTypeName()}'");

            switch (source.SourceKind)
            {
                case SourceKind.Null:
                {
                    Action = SetToNull;
                    return;
                }
                case not SourceKind.Object: throw new InvalidOperationException($"Cannot populate from element of kind '{source.SourceKind}'. Expected Object");
            }

            var populateDeclaredPropertiesAction = GetPopulateDeclaredPropertiesAction(toPopulate, source, typeCache);
            DynamicMemberPopulatorCache? dynamicCache = null;

            async ValueTask<object> populate(object target)
            {
                await populateDeclaredPropertiesAction(target).ConfigureAwait(false);

                if (target is not IDictionary<string, object?> dict)
                    return target;

                dynamicCache ??= new DynamicMemberPopulatorCache();

                for (var index = 0; index < source.Properties.Length; index += 1)
                {
                    var (name, value) = source.Properties[index];
                    if (name is null)
                    {
                        // This property has been handled by the populateDeclaredPropertiesAction
                        continue;
                    }
                    if (dict.TryGetValue(name, out var existingValue) && existingValue is not null)
                    {
                        dict[name] = await GetDynamicValue(name, existingValue, value, dynamicCache, typeCache).ConfigureAwait(false);
                    }
                    else dict[name] = value.GetValue<object>();
                }
                return target;
            }

            Action = populate;
        }

        private static PopulatorAction GetPopulateDeclaredPropertiesAction
        (
            Type toPopulate,
            PopulateSource populateSource,
            TypeCache typeCache
        )
        {
            var declaredProperties = typeCache.GetDeclaredProperties(toPopulate);
            if (populateSource.Properties.Length == 0)
                return DoNothing;
            var actions = new PopulatorAction[populateSource.Properties.Length];
            var actionCount = 0;

            for (var index = 0; index < populateSource.Properties.Length; index += 1)
            {
                var (name, value) = populateSource.Properties[index];
                if (name is null || !declaredProperties.TryGetValue(name, out var declaredProperty))
                {
                    // We have encountered an unknown member in the input JSON. Let's skip it.
                    continue;
                }
                // We set the index to default to signal that this property is handled.
                populateSource.Properties[index] = default;
                actions[actionCount] = GetDeclaredPropertyAction(declaredProperty!, value, typeCache);
                actionCount += 1;
            }

            if (actionCount == 0)
            {
                return DoNothing;
            }

            async ValueTask<object> populateAction(object parent)
            {
                for (var actionIndex = 0; actionIndex < actionCount; actionIndex += 1)
                {
                    await actions[actionIndex].Invoke(parent).ConfigureAwait(false);
                }
                return parent;
            }

            return populateAction;
        }

        private static async ValueTask<object> GetDynamicValue
        (
            string propertyName,
            object existingValue,
            PopulateSource source,
            DynamicMemberPopulatorCache dynamicMemberPopulatorCache,
            TypeCache typeCache
        )
        {
            var existingType = existingValue.GetType();
            if (!typeCache.CanBePopulated(existingType))
            {
                // The existing value cannot be populated, but we know what type is expected.
                return source.GetValue(existingType)!;
            }
            // There is an existing value, and it can be populated. Let's check if we have encountered this property before.
            // if we have, we do not need to construct a new populator for it.
            if (!dynamicMemberPopulatorCache.TryGetValue((propertyName, existingType), out var populator))
                populator = dynamicMemberPopulatorCache[(propertyName, existingType)] = new Populator(existingType, source, typeCache);
            var populatedValue = await populator.PopulateAsync(existingValue).ConfigureAwait(false);
            return populatedValue;
        }

        private static PopulatorAction GetDeclaredPropertyAction(DeclaredProperty declaredProperty, PopulateSource source, TypeCache typeCache)
        {
            // If it's writable and should not be populated, make a set value action for it
            if (declaredProperty.IsWritable && (!declaredProperty.CanBePopulated || declaredProperty.ReplaceOnUpdate))
            {
                var hasPresetValue = false;
                // If the value is null or a value type, we can assign the same (possibly boxed) value to all properties.
                // that value can be established right now, and stored in presetValue.
                object? presetValue = null;
                if (source.SourceKind == SourceKind.Null)
                {
                    hasPresetValue = true;
                }
                else if (declaredProperty.IsValueType)
                {
                    presetValue = source.GetValue(declaredProperty.Type);
                    hasPresetValue = true;
                }

                // Return a set value action for this property
                async ValueTask<object> setValueAction(object parent)
                {
                    var value = hasPresetValue ? presetValue : source.GetValue(declaredProperty.Type);
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
                        var value = source.GetValue(declaredProperty.Type);
                        await declaredProperty.SetValue(parent, value).ConfigureAwait(false);
                    }
                    else
                    {
                        dynamicCache ??= new DynamicMemberPopulatorCache();
                        // There is an existing non-null value, and it can be populated. Let's do it.
                        var value = await GetDynamicValue(declaredProperty.Name, existingValue, source, dynamicCache, typeCache).ConfigureAwait(false);
                        if (declaredProperty.IsWritable) await declaredProperty.SetValue(parent, value).ConfigureAwait(false);
                    }
                    return parent;
                }

                return dynamicPopulateAction;
            }

            // It is a statically known type, and should be populated (we don't know if it's writable). Make a populator for it.
            var populator = new Populator(declaredProperty.Type, source, typeCache);

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
                    var value = source.GetValue(declaredProperty.Type);
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