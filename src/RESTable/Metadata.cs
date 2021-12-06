using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Auth;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable;

/// <inheritdoc />
/// <summary>
///     A resource that creates metadata for the types and resources of the RESTable instance,
///     using the types included in the RESTable.Meta namespace.
/// </summary>
[RESTable(Method.GET, GETAvailableToAll = true)]
public class Metadata : ISelector<Metadata>
{
    public Metadata
    (
        IDictionary<IResource, Method[]> currentAccessScope,
        IEntityResource[] entityResources,
        ITerminalResource[] terminalResources,
        IReadOnlyDictionary<Type, Member[]> entityResourceTypes,
        IReadOnlyDictionary<Type, Member[]> peripheralTypes
    )
    {
        CurrentAccessScope = currentAccessScope;
        EntityResources = entityResources;
        TerminalResources = terminalResources;
        EntityResourceTypes = entityResourceTypes;
        PeripheralTypes = peripheralTypes;
    }

    /// <summary>
    ///     The access scope for the current client
    /// </summary>
    public IDictionary<IResource, Method[]> CurrentAccessScope { get; }

    /// <summary>
    ///     The entity resources within the access scope
    /// </summary>
    public IEntityResource[] EntityResources { get; }

    /// <summary>
    ///     The terminal resources within the access scope
    /// </summary>
    public ITerminalResource[] TerminalResources { get; }

    /// <summary>
    ///     The type list containing all entity resource types
    /// </summary>
    public IReadOnlyDictionary<Type, Member[]> EntityResourceTypes { get; }

    /// <summary>
    ///     The type list containing all peripheral types, types that are
    ///     referenced by some entity resource type or peripheral type.
    /// </summary>
    public IReadOnlyDictionary<Type, Member[]> PeripheralTypes { get; }

    /// <inheritdoc />
    public IEnumerable<Metadata> Select(IRequest<Metadata> request)
    {
        var accessrights = request.Context.Client.AccessRights;
        var typeCache = request.GetRequiredService<TypeCache>();
        yield return GetMetadata(MetadataLevel.Full, accessrights, typeCache);
    }

    public static Metadata GetMetadata(MetadataLevel level, AccessRights rights, TypeCache typeCache)
    {
        var domain = rights.Keys;
        var entityResources = domain
            .OfType<IEntityResource>()
            .Where(r => r.IsGlobal)
            .OrderBy(r => r.Name)
            .ToList();
        var terminalResources = domain
            .OfType<ITerminalResource>()
            .ToList();

        if (level == MetadataLevel.OnlyResources)
            return new Metadata
            (
                new Dictionary<IResource, Method[]>(rights),
                entityResources.ToArray(),
                terminalResources.ToArray(),
                new Dictionary<Type, Member[]>(),
                new Dictionary<Type, Member[]>()
            );

        HashSet<Type> checkedTypes = new();

        void parseType(Type type)
        {
            switch (type)
            {
                case var _ when type.IsEnum:
                    checkedTypes.Add(type);
                    break;
                case var _ when type.IsNullable(out var baseType):
                    parseType(baseType!);
                    break;
                case var _ when type.ImplementsGenericInterface(typeof(IEnumerable<>), out var param) && param!.Any():
                    if (param![0].ImplementsGenericInterface(typeof(IEnumerable<>)))
                        break;
                    parseType(param[0]);
                    break;

                case var _ when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>):
                case var _ when type.IsPrimitive:
                case var _ when type == typeof(object):
                    break;

                case var _ when checkedTypes.Add(type):
                {
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => !p.RESTableIgnored())
                        .Select(p => p.FieldType);
                    foreach (var field in fields)
                        parseType(field);
                    var properties = typeCache.GetDeclaredProperties(type).Values
                        .Where(p => !p.Hidden)
                        .Select(p => p.Type);
                    foreach (var property in properties)
                        parseType(property);
                    break;
                }
            }
        }

        var entityTypes = entityResources.Select(r => r.Type).ToHashSet();
        var terminalTypes = terminalResources.Select(r => r.Type).ToHashSet();
        foreach (var type in entityTypes)
            parseType(type);
        checkedTypes.ExceptWith(entityTypes);
        foreach (var type in terminalTypes)
            parseType(type);
        checkedTypes.ExceptWith(terminalTypes);

        var entityResourceTypes = entityTypes.ToDictionary(t => t, type => typeCache.GetDeclaredProperties(type).Values.Cast<Member>().ToArray());
        var peripheralTypes = checkedTypes.ToDictionary(t => t, type =>
        {
            var props = typeCache.GetDeclaredProperties(type).Values;
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !p.RESTableIgnored())
                .Select(f => new Field(f));
            return props.Union<Member>(fields).ToArray();
        });

        return new Metadata
        (
            new Dictionary<IResource, Method[]>(rights),
            entityResources.ToArray(),
            terminalResources.ToArray(),
            new ReadOnlyDictionary<Type, Member[]>(entityResourceTypes),
            new ReadOnlyDictionary<Type, Member[]>(peripheralTypes)
        );
    }
}