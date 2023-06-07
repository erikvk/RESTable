using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RESTable.Meta.IL;
using RESTable.Meta.Internal;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Results;

namespace RESTable.Meta;

/// <summary>
///     The type cache keeps track of discovered types and provides
///     fast access to their declared properties.
/// </summary>
/// limit
public class TypeCache
{
    public TypeCache(IEnumerable<IEntityTypeContractResolver> entityTypeContractResolvers, ResourceCollection resourceCollection, TermCache termCache)
    {
        EntityTypeContractResolvers = entityTypeContractResolvers;
        ResourceCollection = resourceCollection;
        DeclaredPropertyCache = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, DeclaredProperty>>();
        DeclaredPropertyCacheByActualName = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, DeclaredProperty>>();
        CanBePopulatedCache = new ConcurrentDictionary<Type, bool>();
        EntityTypeContracts = new ConcurrentDictionary<Type, EntityTypeContract>();
        TermFactory = new TermFactory(this, termCache, resourceCollection);
    }

    internal ConcurrentDictionary<Type, IReadOnlyDictionary<string, DeclaredProperty>> DeclaredPropertyCache { get; }
    private ConcurrentDictionary<Type, IReadOnlyDictionary<string, DeclaredProperty>> DeclaredPropertyCacheByActualName { get; }
    private ConcurrentDictionary<Type, bool> CanBePopulatedCache { get; }
    private ConcurrentDictionary<Type, EntityTypeContract> EntityTypeContracts { get; }
    private IEnumerable<IEntityTypeContractResolver> EntityTypeContractResolvers { get; }
    internal TermFactory TermFactory { get; }
    private ResourceCollection ResourceCollection { get; }

    #region Types

    public bool CanBePopulated(Type type)
    {
        bool getValue()
        {
            return !type.IsValueType &&
                   Type.GetTypeCode(type) == TypeCode.Object &&
                   (type.IsDictionary(out var writeAble, out _) && writeAble || !type.ImplementsEnumerableInterface(out _));
        }

        if (!CanBePopulatedCache.TryGetValue(type, out var value)) value = CanBePopulatedCache[type] = getValue();
        return value;
    }

    public EntityTypeContract GetEntityTypeContract(Type type)
    {
        if (EntityTypeContracts.TryGetValue(type, out var value))
            return value;
        GetDeclaredProperties(type);
        return EntityTypeContracts[type];
    }

    #endregion

    #region Declared properties

    private IEnumerable<DeclaredProperty> FindAndParseDeclaredProperties(Type type, bool flag = false)
    {
        if (type.HasAttribute<RESTableMemberAttribute>(out var memberAttribute) && memberAttribute!.Ignored)
            return Array.Empty<DeclaredProperty>();
        return ParseDeclaredProperties(type.GetProperties(BindingFlags.Public | BindingFlags.Instance), flag);
    }

    private IEnumerable<DeclaredProperty> ParseDeclaredProperties(IEnumerable<PropertyInfo> props, bool flag)
    {
        var baseEnumeration = props
            .Where(p => !p.RESTableIgnored())
            .Where(p => !p.GetIndexParameters().Any())
            .Where(p => !p.PropertyType.HasAttribute<RESTableIgnoreMembersWithTypeAttribute>())
            .Select(p => new DeclaredProperty(p, flag))
            .OrderBy(p => p.Order);
        foreach (var property in baseEnumeration)
            // Each thing we yield in this loop is a member of the owner
            if (property.MergeOntoOwner)
            {
                // Each property of this property should be merged on the owner. We refer to the
                var propertyType = property.Type;
                if (property.Getter is not { } propertyGetter)
                    // No point in setting up merging if we can't access the merged object
                    continue;
                foreach (var propertyProperty in GetDeclaredProperties(property.Type).Values)
                {
                    if (propertyProperty.Getter is { } propertyPropertyGetter)
                        propertyProperty.Getter = async ownerTarget =>
                        {
                            var propertyValue = await propertyGetter.Invoke(ownerTarget).ConfigureAwait(false);
                            if (propertyValue is null)
                                return null;
                            return await propertyPropertyGetter(propertyValue).ConfigureAwait(false);
                        };
                    if (propertyProperty.Setter is { } propertyPropertySetter)
                        propertyProperty.Setter = async (owner, propertyPropertyValue) =>
                        {
                            var propertyValue = await propertyGetter.Invoke(owner).ConfigureAwait(false);
                            if (propertyValue is null)
                            {
                                if (property.Setter is null)
                                    // Not much we can do if we can't set the property value to the owner, even if we 
                                    // can set the property property value.
                                    return;
                                // If the property's value is null, we need to create it to be able 
                                // to set one of its properties. If this fails, we have a design error.
                                propertyValue = Activator.CreateInstance(propertyType);
                                if (propertyValue is null)
                                    // Activation yielded null, not much we can do then.
                                    return;
                                await property.Setter(owner, propertyValue).ConfigureAwait(false);
                            }
                            await propertyPropertySetter(propertyValue, propertyPropertyValue).ConfigureAwait(false);
                        };
                    yield return propertyProperty;
                }
            }
            else
            {
                yield return property;
            }
    }

    /// <summary>
    ///     Gets the declared properties for a given type
    /// </summary>
    public IReadOnlyDictionary<string, DeclaredProperty> GetDeclaredProperties(Type? type, bool groupByActualName = false)
    {
        IEnumerable<DeclaredProperty> Make(Type? _type)
        {
            switch (_type)
            {
                case null:
                case { } when _type.HasAttribute<RESTableIgnoreMembersWithTypeAttribute>(): return Array.Empty<DeclaredProperty>();
                case { } when _type.IsDictionary(out _, out _):
                {
                    return FindAndParseDeclaredProperties(_type, true).Select(p =>
                    {
                        p.Hidden = !p.HasAttribute<RESTableMemberAttribute>(out var attr) || attr!.Hidden != false;
                        return p;
                    });
                }
                case { IsInterface: true }:
                {
                    return ParseDeclaredProperties
                    (
                        new[] { _type }
                            .Concat(_type.GetInterfaces())
                            .SelectMany(i => i.GetProperties(BindingFlags.Instance | BindingFlags.Public)),
                        false
                    );
                }
                case { } when _type.GetRESTableInterfaceType() is Type t:
                {
                    var interfaceName = t.GetRESTableTypeName();
                    var targetsByProp = _type
                        .GetInterfaceMap(t)
                        .TargetMethods
                        .GroupBy(m =>
                        {
                            if (m.IsPrivate && m.Name.StartsWith($"{interfaceName}.get_"))
                                return m.Name.Split(interfaceName + ".get_")[1];
                            if (m.IsPrivate && m.Name.StartsWith($"{interfaceName}.set_"))
                                return m.Name.Split(interfaceName + ".set_")[1];
                            if (m.Name.StartsWith("get_"))
                                return m.Name.Split("get_")[1];
                            if (m.Name.StartsWith("set_"))
                                return m.Name.Split("set_")[1];
                            return null;
                        })
                        .Where(group => group.Key is not null)
                        .ToDictionary(m => m.Key!, m => (
                            getter: m.FirstOrDefault(p => p.GetParameters().Length == 0),
                            setter: m.FirstOrDefault(p => p.GetParameters().Length == 1)
                        ));
                    return Make(t).Select(p =>
                    {
                        var (getter, setter) = targetsByProp.SafeGet(p.ActualName);
                        if (p.IsReadable)
                            p.ActualName = getter!.GetInstructions()
                                .Select(i => i.OpCode == OpCodes.Call && i.Operand is MethodInfo calledMethod && getter!.IsSpecialName
                                    ? type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .FirstOrDefault(prop => prop.GetGetMethod() == calledMethod)
                                    : null)
                                .LastOrDefault(prop => prop is not null)?
                                .Name!;
                        else if (p.IsWritable)
                            p.ActualName = setter!.GetInstructions()
                                .Select(i => i.OpCode == OpCodes.Call && i.Operand is MethodInfo calledMethod && setter!.IsSpecialName
                                    ? type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .FirstOrDefault(prop => prop.GetSetMethod() == calledMethod)
                                    : null)
                                .LastOrDefault(prop => prop is not null)?
                                .Name!;
                        return p;
                    });
                }
                case { } when _type.IsSubclassOf(typeof(Terminal)):
                {
                    return FindAndParseDeclaredProperties(_type).Except(GetDeclaredProperties(typeof(Terminal)).Values, DeclaredProperty.NameComparer);
                }
                case { } when _type.IsNullable(out var underlying):
                {
                    return GetDeclaredProperties(underlying).Values;
                }
                case { } when _type.HasAttribute<RESTableViewAttribute>():
                {
                    return FindAndParseDeclaredProperties(_type).Union(Make(_type.DeclaringType));
                }
                case { }: return FindAndParseDeclaredProperties(_type);
            }
        }

        if (type?.GetRESTableTypeName() is null)
            throw new Exception("Could not get declared properties for unknown type");

        if (!groupByActualName)
        {
            if (!DeclaredPropertyCache.TryGetValue(type, out var propsByName))
            {
                var propertyList = Make(type).ToList();
                var contract = EntityTypeContracts[type] = new EntityTypeContract(type, propertyList);
                foreach (var resolver in EntityTypeContractResolvers)
                    resolver.ResolveContract(contract);
                var propertyDictionary = new Dictionary<string, DeclaredProperty>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in propertyList)
                {
                    var counter = 0;
                    var key = property.Name;
                    if (propertyDictionary.ContainsKey(key))
                    {
                        while (propertyDictionary.ContainsKey($"{key}\\{counter}"))
                            counter += 1;
                        key = $"{key}\\{counter}";
                    }
                    propertyDictionary[key] = property;
                }
                propsByName = DeclaredPropertyCache[type] = propertyDictionary;
            }
            return propsByName;
        }

        if (!DeclaredPropertyCacheByActualName.TryGetValue(type, out var propsByActualName))
            propsByActualName = DeclaredPropertyCacheByActualName[type] = GetDeclaredProperties(type)
                .Values
                .ToDictionary(p => p.ActualName, StringComparer.OrdinalIgnoreCase);
        return propsByActualName;
    }

    public DeclaredProperty FindDeclaredProperty(Type type, string key)
    {
        if (TryFindDeclaredProperty(type, key, out var property)) return property!;
        if (type.IsNullable(out var underlying))
            type = underlying!;
        var resource = ResourceCollection.SafeGetResource(type);
        throw new UnknownProperty(type, resource, key);
    }

    public bool TryFindDeclaredProperty(Type type, string key, out DeclaredProperty? declaredProperty)
    {
        if (!type.IsDictionary(out _, out _) && type.ImplementsEnumerableInterface(out var parameter))
        {
            var collectionReadonly = typeof(IList).IsAssignableFrom(type) || type.ImplementsGenericInterface(typeof(IList<>));
            switch (key)
            {
                case "-":
                {
                    declaredProperty = new LastIndexProperty(parameter!, collectionReadonly, type);
                    return true;
                }
                case var _ when int.TryParse(key, out var integer):
                {
                    declaredProperty = new IndexProperty(integer, key, parameter!, collectionReadonly, type);
                    return true;
                }
            }
        }

        if (GetDeclaredProperties(type).TryGetValue(key, out var prop))
        {
            declaredProperty = prop;
            return true;
        }
        declaredProperty = null;
        return false;
    }

    #endregion
}
