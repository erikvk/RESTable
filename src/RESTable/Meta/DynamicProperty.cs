using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;

namespace RESTable.Meta;

/// <inheritdoc />
/// <summary>
///     A dynamic property represents a dynamic property of a class, that is,
///     a member that is not known at compile time.
/// </summary>
public class DynamicProperty : Property
{
    /// <summary>
    ///     Should the evaluation fall back to a declared property if no dynamic property
    ///     can be found in the target entity?
    /// </summary>
    public readonly bool DeclaredFallback;

    private DynamicProperty(string name, bool declaredFallback) : base(null)
    {
        Name = ActualName = name;
        DeclaredFallback = declaredFallback;
        Type = typeof(object);
        var typeCache = ApplicationServicesAccessor.GetRequiredService<TypeCache>();
        var jsonProvider = ApplicationServicesAccessor.GetRequiredService<IJsonProvider>();

        async ValueTask<object?> getValue(object obj)
        {
            object? value;
            string? actualKey;

            async ValueTask<object?> getFromDeclared()
            {
                var type = obj.GetType();
                typeCache.TryFindDeclaredProperty(type, Name, out var prop);
                actualKey = prop?.Name;
                if (prop is null)
                    value = null;
                else value = await prop.GetValue(obj).ConfigureAwait(false);
                Name = actualKey ?? Name;
                return value;
            }

            switch (obj)
            {
                case IDynamicMemberValueProvider dynamicMemberValueProvider:
                {
                    if (dynamicMemberValueProvider.TryGetValue(Name, out value, out actualKey))
                    {
                        Name = actualKey!;
                        return value;
                    }
                    return DeclaredFallback ? await getFromDeclared().ConfigureAwait(false) : null;
                }
                case JsonElement { ValueKind: JsonValueKind.Object } element:
                {
                    foreach (var jsonProperty in element.EnumerateObject())
                        if (string.Equals(jsonProperty.Name, Name, StringComparison.OrdinalIgnoreCase))
                        {
                            // Set the name of this dynamic property after the actual name of the property
                            Name = jsonProperty.Name;
                            return jsonProvider.ToObject<object?>(jsonProperty.Value);
                        }
                    return DeclaredFallback ? await getFromDeclared().ConfigureAwait(false) : null;
                }
                case IDictionary<string, object?> dict:
                {
                    var capitalized = Name.Capitalize();
                    if (dict.TryGetValue(capitalized, out value))
                    {
                        Name = capitalized;
                        return value;
                    }
                    if (dict.TryFindInDictionary(Name, out actualKey, out value))
                    {
                        Name = actualKey!;
                        return value;
                    }
                    return DeclaredFallback ? await getFromDeclared().ConfigureAwait(false) : null;
                }
                case IDictionary dict:
                {
                    var capitalized = Name.Capitalize();
                    if (dict.Contains(capitalized))
                    {
                        value = dict[capitalized];
                        Name = capitalized;
                        return value;
                    }
                    if (dict.TryFindInDictionary(Name, out actualKey, out value))
                    {
                        Name = actualKey!;
                        return value;
                    }
                    return DeclaredFallback ? await getFromDeclared().ConfigureAwait(false) : null;
                }
                default: return await getFromDeclared().ConfigureAwait(false);
            }
        }

        Getter = getValue;

        async ValueTask setValue(object obj, object? value)
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
                default:
                    var type = obj.GetType();
                    try
                    {
                        typeCache.TryFindDeclaredProperty(type, Name, out var property);
                        if (property is null) return;
                        await property.SetValue(obj, value).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Setting the value could fail due to access restrictions, in which case we just return
                    }
                    break;
            }
        }

        Setter = setValue;
    }

    /// <inheritdoc />
    public sealed override string Name { get; internal set; }

    /// <inheritdoc />
    public sealed override string ActualName { get; internal set; }

    /// <inheritdoc />
    public sealed override Type Type { get; protected set; }

    /// <inheritdoc />
    public override bool IsDynamic => true;

    /// <summary>
    ///     Creates a dynamic property instance from a key string
    /// </summary>
    /// <param name="keyString">The string to parse</param>
    /// <param name="declaredFallback">
    ///     Should the evaluation fall back to a declared property
    ///     if no dynamic property can be found in the target entity?
    /// </param>
    /// <returns>
    ///     A dynamic property that represents the runtime property
    ///     described by the key string
    /// </returns>
    public static DynamicProperty Parse(string keyString, bool declaredFallback = false)
    {
        return new(keyString, declaredFallback);
    }
}
