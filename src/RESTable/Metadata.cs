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

namespace RESTable
{
    /// <inheritdoc />
    /// <summary>
    /// A resource that creates metadata for the types and resources of the RESTable instance,
    /// using the types included in the RESTable.Meta namespace.
    /// </summary>
    [RESTable(Method.GET, GETAvailableToAll = true)]
    public class Metadata : ISelector<Metadata>
    {
        /// <summary>
        /// The access scope for the current client
        /// </summary>
        public IDictionary<IResource, Method[]> CurrentAccessScope { get; private set; }

        /// <summary>
        /// The entity resources within the access scope
        /// </summary>
        public IEntityResource[] EntityResources { get; private set; }

        /// <summary>
        /// The terminal resources within the access scope
        /// </summary>
        public ITerminalResource[] TerminalResources { get; private set; }

        /// <summary>
        /// The type list containing all entity resource types
        /// </summary>
        public IReadOnlyDictionary<Type, Member[]> EntityResourceTypes { get; private set; }

        /// <summary>
        /// The type list containing all peripheral types, types that are 
        /// referenced by some entity resource type or peripheral type.
        /// </summary>
        public IReadOnlyDictionary<Type, Member[]> PeripheralTypes { get; private set; }

        /// <inheritdoc />
        public IEnumerable<Metadata> Select(IRequest<Metadata> request)
        {
            var accessrights = request.Context.Client.AccessRights;
            var rootAccess = request.GetRequiredService<RootAccess>();
            var resourceCollection = request.GetRequiredService<ResourceCollection>();
            var typeCache = request.GetRequiredService<TypeCache>();
            yield return GetMetadata(MetadataLevel.Full, accessrights, rootAccess, resourceCollection, typeCache);
        }

        public static Metadata GetMetadata(MetadataLevel level, AccessRights rights, RootAccess rootAccess, ResourceCollection resourceCollection, TypeCache typeCache)
        {
            var domain = rights?.Keys ?? resourceCollection.AllResources;
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
                {
                    CurrentAccessScope = new Dictionary<IResource, Method[]>(rights ?? rootAccess),
                    EntityResources = entityResources.ToArray(),
                    TerminalResources = terminalResources.ToArray()
                };

            var checkedTypes = new HashSet<Type>();

            void parseType(Type type)
            {
                switch (type)
                {
                    case var _ when type.IsEnum:
                        checkedTypes.Add(type);
                        break;
                    case var _ when type.IsNullable(out var baseType):
                        parseType(baseType);
                        break;
                    case var _ when type.ImplementsGenericInterface(typeof(IEnumerable<>), out var param) && param.Any():
                        if (param[0].ImplementsGenericInterface(typeof(IEnumerable<>)))
                            break;
                        parseType(param[0]);
                        break;

                    case var _ when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>):
                    case var _ when IsPrimitive(type):
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

            return new Metadata
            {
                CurrentAccessScope = new Dictionary<IResource, Method[]>(rights ?? rootAccess),
                EntityResources = entityResources.ToArray(),
                TerminalResources = terminalResources.ToArray(),
                EntityResourceTypes = new ReadOnlyDictionary<Type, Member[]>(entityTypes.ToDictionary(t => t, type =>
                    typeCache.GetDeclaredProperties(type).Values.Cast<Member>().ToArray())),
                PeripheralTypes = new ReadOnlyDictionary<Type, Member[]>(checkedTypes.ToDictionary(t => t, type =>
                {
                    var props = typeCache.GetDeclaredProperties(type).Values;
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => !p.RESTableIgnored())
                        .Select(f => new Field(f));
                    return props.Union<Member>(fields).ToArray();
                }))
            };
        }

        private Metadata() { }

        private static bool IsPrimitive(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Empty:
                case TypeCode.DBNull:
                case TypeCode.Boolean:
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                case TypeCode.String: return true;
                default: return false;
            }
        }
    }
}