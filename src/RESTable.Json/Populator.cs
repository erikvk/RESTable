using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using RESTable.Meta;

namespace RESTable.Json
{
    public readonly struct Populator
    {
        private Func<object?, ValueTask<object?>> Action { get; }

        private static async ValueTask PopulateCollection<TElement>
        (
            ICollection<TElement?> existingCollection,
            JsonElement.ArrayEnumerator newValues,
            TypeCache typeCache,
            JsonSerializerOptions options
        )
        {
            // First let's see if values of type TElement can even be populated. If not, we clear and add all new ones.
            if (!typeof(TElement).CanBePopulated())
            {
                existingCollection.Clear();
                foreach (var newValue in newValues)
                {
                    var value = newValue.ToObject<TElement>(options);
                    existingCollection.Add(value);
                }
            }

            using var newEnumerator = newValues.GetEnumerator();

            if (!newEnumerator.MoveNext())
            {
                // New collection was empty. Just clear and be done.
                existingCollection.Clear();
                return;
            }

            using var exiEnumerator = existingCollection.GetEnumerator();

            do
            {
                if (!exiEnumerator.MoveNext() || exiEnumerator.Current is null)
                {
                    // There is no existing value to populate the current new value to, so we create a new one
                    var newValue = newEnumerator.Current.ToObject<TElement>(options);
                    existingCollection.Add(newValue);
                }
                else
                {
                    // Create a populator for the existing object, and recusively populate it from the new 
                    // value.
                }
            } while (newEnumerator.MoveNext());

            // Any additional items in the existing collection are left in place
        }

        private static async ValueTask PopulateDynamic<TValue>(TValue value, JsonElement newValue, TypeCache typeCache, JsonSerializerOptions options)
        {
            // Recusively create a populator for the existing object, and populate it from the new 
            // value.
        }

        private static async ValueTask PopulateDictionary(IDictionary<string, object?> dictionary, IEnumerable<JsonProperty> newProperties, TypeCache typeCache,
            JsonSerializerOptions options)
        {
            foreach (var newProperty in newProperties)
            {
                // If there is an existing value for the property, which is not null and can be populated, we populate it
                if (dictionary.TryGetValue(newProperty.Name, out var existingValue) && existingValue is not null && existingValue.GetType().CanBePopulated())
                {
                    ValueTask updateTask = PopulateDynamic((dynamic) existingValue, newProperty.Value, typeCache, options);
                    await updateTask.ConfigureAwait(false);
                }
                // Else we just overwrite it
                else
                {
                    var newValue = newProperty.Value.ToObject<object>(options);
                    dictionary[newProperty.Name] = newValue;
                }
            }
        }

        public Populator(Type toPopulate, JsonElement jsonElement, TypeCache typeCache, JsonSerializerOptions options)
        {
            if (!toPopulate.CanBePopulated())
                throw new InvalidOperationException($"Cannot populate onto type '{toPopulate.GetRESTableTypeName()}'");

            if (jsonElement.ValueKind == JsonValueKind.Null)
            {
                Action = _ => default;
                return;
            }

            var declaredProperties = typeCache.GetDeclaredProperties(toPopulate);

            #region Dictionary

            if (toPopulate.IsDictionary())
            {
                if (jsonElement.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException($"Cannot populate object from a JsonElement with ValueKind '{jsonElement.ValueKind}'. Expected Object");
                }

                var index = new Dictionary<string, (Type ExpectedType, Populator Populator)>(StringComparer.OrdinalIgnoreCase);

                async ValueTask<object?> PopulateDictionary(object? target)
                {
                    if (target is null)
                    {
                        // Nothing to populate, let's create a new instance
                        return jsonElement.ToObject(toPopulate, options);
                    }
                    if (target is not IDictionary<string, object?> dict)
                    {
                        throw new InvalidOperationException("Type mismatch when populating object. Expected Dictionary");
                    }

                    foreach (var property in jsonElement.EnumerateObject())
                    {
                        var propertyName = property.Name;

                        if (dict.TryGetValue(propertyName, out var existing) && existing is not null)
                        {
                            var existingType = existing.GetType();
                            if (!existingType.CanBePopulated())
                            {
                                // The existing value cannot be populated, but we know what type is expected.
                                dict[propertyName] = property.Value.ToObject(existingType, options);
                            }
                            else
                            {
                                // There is an existing value, and it can be populated. Let's check if we 
                                // have encountered this property before.
                                if (!index.TryGetValue(propertyName, out var indexValue) && indexValue.ExpectedType == existingType)
                                {
                                    // We have not, but let's index it now
                                    indexValue = index[propertyName] = (existingType, new Populator(existingType, property.Value, typeCache, options));
                                }
                                var populatedValue = await indexValue.Populator.PopulateAsync(existing).ConfigureAwait(false);
                                dict[propertyName] = populatedValue;
                            }
                        }
                        else
                        {
                            if (declaredProperties.TryGetValue(propertyName, out var declaredProperty))
                            {
                                // This is an assignment to a declared property of this dictionary type. We skip
                                // it unless it's writable.
                                if (declaredProperty!.IsWritable)
                                {
                                    if (!declaredProperty.CanBePopulated)
                                    {
                                        var value = property.Value.ToObject(declaredProperty.Type, options);
                                        await declaredProperty.SetValue(target, value).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        var existingValue = await declaredProperty.GetValue(target).ConfigureAwait(false);
                                        if (existingValue is null)
                                        {
                                            var value = property.Value.ToObject(declaredProperty.Type, options);
                                            await declaredProperty.SetValue(target, value).ConfigureAwait(false);
                                        }
                                        else
                                        {
                                            // There is an existing non-null value, and it can be populated. Let's check if we 
                                            // have encountered this property before.
                                            if (!index.TryGetValue(propertyName, out var indexValue) && indexValue.ExpectedType == declaredProperty.Type)
                                            {
                                                // We have not, but let's index it now
                                                indexValue = index[propertyName] =
                                                    (declaredProperty.Type, new Populator(declaredProperty.Type, property.Value, typeCache, options));
                                            }
                                            var populatedValue = await indexValue.Populator.PopulateAsync(existingValue).ConfigureAwait(false);
                                            await declaredProperty.SetValue(target, populatedValue).ConfigureAwait(false);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // No existing value exists for this member. That means we have no knowledge
                                // of what type we should create. Let's just create whatever's reasonable.
                                dict[propertyName] = property.Value.ToObject<object>(options);
                            }
                        }
                    }
                    return target;
                }

                Action = PopulateDictionary;
                return;
            }

            #endregion

            #region Collections

            if (toPopulate.ImplementsGenericInterface(typeof(IEnumerable<>), out var parameters))
            {
                if (jsonElement.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException($"Cannot populate enumerable value from a JsonElement with ValueKind '{jsonElement.ValueKind}'. Expected Array");
                }
                var typeParameter = parameters![0];
                if (typeParameter.CanBePopulated()) { }
                // We can't populate individual elements. Instead we must replace at index.
                else { }
                Action = null!;
                return;
            }
            else if (toPopulate.ImplementsGenericInterface(typeof(IAsyncEnumerable<>), out parameters))
            {
                // This is an async enumerable type
                Action = null!;
                return;
            }

            #endregion

            #region Regular class

            {
                if (jsonElement.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException($"Cannot populate object from a JsonElement with ValueKind '{jsonElement.ValueKind}'. Expected Object");
                }

                var index = new Dictionary<string, Populator>(StringComparer.OrdinalIgnoreCase);

                async ValueTask<object?> PopulateObject(object? target)
                {
                    if (target is null)
                    {
                        // Nothing to populate, let's create a new instance
                        return jsonElement.ToObject(toPopulate, options);
                    }

                    foreach (var property in jsonElement.EnumerateObject())
                    {
                        var propertyName = property.Name;

                        if (!declaredProperties.TryGetValue(propertyName, out var declaredProperty))
                        {
                            // We have encountered an unknown member in the input JSON. Let's skip it.
                            continue;
                        }

                        if (declaredProperty!.IsWritable && (!declaredProperty.CanBePopulated || declaredProperty.ReplaceOnUpdate))
                        {
                            var value = property.Value.ToObject(declaredProperty.Type, options);
                            await declaredProperty.SetValue(target, value).ConfigureAwait(false);
                        }
                        else
                        {
                            var existingValue = await declaredProperty.GetValue(target).ConfigureAwait(false);
                            if (existingValue is null && declaredProperty.IsWritable)
                            {
                                var value = property.Value.ToObject(declaredProperty.Type, options);
                                await declaredProperty.SetValue(target, value).ConfigureAwait(false);
                            }
                            else
                            {
                                // There is an existing non-null value, and it can be populated. Let's check if we 
                                // have encountered this property before.
                                if (!index.TryGetValue(propertyName, out var populator))
                                {
                                    // We have not, but let's index it now
                                    populator = index[propertyName] = new Populator(declaredProperty.Type, property.Value, typeCache, options);
                                }
                                var populatedValue = await populator.PopulateAsync(existingValue).ConfigureAwait(false);
                                if (declaredProperty.IsWritable)
                                    await declaredProperty.SetValue(target, populatedValue).ConfigureAwait(false);
                            }
                        }
                    }
                    return target;
                }

                Action = PopulateObject;
                return;

                #endregion
            }
        }

        // Take a target object and populate it, returning a populated version
        public async ValueTask<object?> PopulateAsync(object? target) => await Action(target).ConfigureAwait(false);

//
//            foreach (var property in)
//            {
//                if (declaredProperties.TryGetValue(property.Name, out var declaredProperty))
//                {
//                    if (!declaredProperty!.IsWritable)
//                    {
//                        // Skip declared properties that cannot be written to
//                        continue;
//                    }
//                    switch (property.Value.ValueKind)
//                    {
//                        // If the kind is undefined we just skip
//                        case JsonValueKind.Undefined: continue;
//
//                        // If we're writing an array to a property that is a collection and has ReplaceOnUpdate set to false, we 
//                        // populate any existing values instead of replacing it.
//                        case JsonValueKind.Array when declaredProperty is {IsCollection: true, ReplaceOnUpdate: false}:
//                        {
//                            async ValueTask UpdateAction(T target)
//                            {
//                                var targetCollection = await declaredProperty.GetValue(target!).ConfigureAwait(false);
//                                if (targetCollection is null)
//                                {
//                                    // Value was null. Just take the new value and set the property to that.
//                                    targetCollection = property.Value.ToObject(declaredProperty.Type, options);
//                                    await declaredProperty.SetValue(target!, targetCollection).ConfigureAwait(false);
//                                    return;
//                                }
//                                try
//                                {
//                                    // Let's try and clear the collection and fill it from the input JSON.
//                                    ValueTask populateTask = PopulateCollection((dynamic) targetCollection, property.Value.EnumerateArray(), typeCache, options);
//                                    await populateTask.ConfigureAwait(false);
//                                }
//                                catch (RuntimeBinderException)
//                                {
//                                    // We couldn't treat the target collection as an ICollection{T}.
//                                    throw new InvalidOperationException($"Could not populate onto the member '{declaredProperty.Name}' of type " +
//                                                                        $"'{typeof(T).GetRESTableTypeName()}' marked as ReplaceOnUpdate: false, " +
//                                                                        "since the value obtained from this member could not be converted to ICollection<T>.");
//                                }
//                            }
//
//                            subActions.Add(UpdateAction);
//                            break;
//                        }
//
//                        // If we're writing an object to a dictionary property that has ReplaceOnUpdate set to false, we
//                        // populate any existing value (as in clear + add) instead of replacing it.
//                        case JsonValueKind.Object when declaredProperty is {IsCollection: true, ReplaceOnUpdate: false} p && p.Type.IsDictionary():
//                        {
//                            async ValueTask UpdateAction(T target)
//                            {
//                                var targetDictionary = await declaredProperty.GetValue(target!).ConfigureAwait(false);
//                                if (targetDictionary is null)
//                                {
//                                    // Value was null. Just take the new value and set the property to that.
//                                    targetDictionary = property.Value.ToObject(declaredProperty.Type, options);
//                                    await declaredProperty.SetValue(target!, targetDictionary).ConfigureAwait(false);
//                                    return;
//                                }
//                                try
//                                {
//                                    // Let's try and clear the collection and fill it from the input JSON.
//                                    var populateTask = PopulateDictionary((IDictionary<string, object?>) targetDictionary, property.Value.EnumerateObject(), typeCache, options);
//                                    await populateTask.ConfigureAwait(false);
//                                }
//                                catch (RuntimeBinderException)
//                                {
//                                    // We couldn't treat the target collection as an ICollection{T}.
//                                    throw new InvalidOperationException($"Could not populate onto the member '{declaredProperty.Name}' of type " +
//                                                                        $"'{typeof(T).GetRESTableTypeName()}' marked as ReplaceOnUpdate: false, " +
//                                                                        "since the value obtained from this member could not be converted to ICollection<T>.");
//                                }
//                            }
//
//                            subActions.Add(UpdateAction);
//                            break;
//                        }
//
//                        // We allow visible collections to be updated from an object, and pick up indexes to update from object property names.s
//                        case JsonValueKind.Object when declaredProperty is {IsCollection: true}:
//                        {
//                            async ValueTask UpdateAction(T target) { }
//                        }
//
//
//                        default:
//                        {
//                            var declaredPropertyValue = property.Value.ToObject(declaredProperty.Type, options);
//                            subActions.Add(target => declaredProperty.SetValue(target, declaredPropertyValue));
//                            break;
//                        }
//                    }
//                }
//                else if (isDynamic)
//                {
//                    // Treat unknown properties as dynamic
//                    var dynamicProperty = DynamicProperty.Parse(property.Name);
//                    var dynamicPropertyValue = property.Value.ToObject<object>(options);
//                    subActions.Add((dynamicProperty, dynamicPropertyValue));
//                }
//            }
//            Action = subActions.ToArray();
//        }
    }
}