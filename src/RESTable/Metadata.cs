﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Internal.Auth;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.Linq;

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
            yield return Make(MetadataLevel.Full, accessrights, request.GetService<RESTableConfigurator>());
        }

        /// <summary>
        /// Generates metadata according to a given metadata level
        /// </summary>
        public static Metadata Get(MetadataLevel level, RESTableConfigurator configurator) => Make(level, null, configurator);

        internal static Metadata Make(MetadataLevel level, AccessRights rights, RESTableConfigurator configurator)
        {
            var resourceCollection = configurator.ResourceCollection;
            var typeCache = configurator.TypeCache;
            var rootAccess = configurator.RootAccess;
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
                        type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => !p.RESTableIgnored())
                            .Select(p => p.FieldType)
                            .ForEach(parseType);
                        typeCache.GetDeclaredProperties(type).Values
                            .Where(p => !p.Hidden)
                            .Select(p => p.Type)
                            .ForEach(parseType);
                        break;
                }
            }

            var entityTypes = entityResources.Select(r => r.Type).ToHashSet();
            var terminalTypes = terminalResources.Select(r => r.Type).ToHashSet();
            entityTypes.ForEach(parseType);
            checkedTypes.ExceptWith(entityTypes);
            terminalTypes.ForEach(parseType);
            checkedTypes.ExceptWith(terminalTypes);

            return new Metadata
            {
                CurrentAccessScope = new Dictionary<IResource, Method[]>(rights ?? configurator.RootAccess),
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