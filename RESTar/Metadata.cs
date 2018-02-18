using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Auth;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Results.Error;

namespace RESTar
{
    /// <summary>
    /// The levels of metadata that can be used by the Metadata resource
    /// </summary>
    public enum MetadataLevel
    {
        /// <summary>
        /// Only resource lists (EntityResources and TerminalResources) are populated
        /// </summary>
        OnlyResources,

        /// <summary>
        /// Resource lists and type lists (including members) are populated. Type lists cover the 
        /// entire type tree (types used in resource types, etc.)
        /// </summary>
        Full
    }

    /// <inheritdoc />
    /// <summary>
    /// Creates metadata for the types and resources of the RESTar instance
    /// </summary>
    [RESTar(Methods.GET, GETAvailableToAll = true)]
    public class Metadata : ISelector<Metadata>
    {
        /// <summary>
        /// The access scope for the current client
        /// </summary>
        public IDictionary<IResource, Methods[]> CurrentAccessScope { get; private set; }

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
        public (Type Type, Member[] Members)[] EntityResourceTypes { get; private set; }

        /// <summary>
        /// The type list containing all peripheral types, types that are 
        /// referenced by some entity resource type or peripheral type.
        /// </summary>
        public (Type Type, Member[] Members)[] PeripheralTypes { get; private set; }

        /// <inheritdoc />
        public IEnumerable<Metadata> Select(IRequest<Metadata> request)
        {
            var accessrights = Authenticator.AuthTokens[request.AuthToken];
            return new[] {Make(MetadataLevel.Full, accessrights)};
        }

        /// <summary>
        /// Generates metadata according to a given metadata level
        /// </summary>
        public static Metadata Get(MetadataLevel level) => Make(level, null);

        internal static Metadata Make(MetadataLevel level, AccessRights rights)
        {
            if (!RESTarConfig.Initialized) throw new NotInitialized();
            var domain = rights?.Keys ?? RESTarConfig.Resources;
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
                    CurrentAccessScope = new Dictionary<IResource, Methods[]>(rights ?? AccessRights.Root),
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
                    case var _ when type.Implements(typeof(IEnumerable<>), out var param):
                        if (param[0].Implements(typeof(IEnumerable<>)))
                            break;
                        parseType(param[0]);
                        break;

                    case var _ when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>):
                    case var _ when IsPrimitive(type):
                    case var _ when type == typeof(object):
                        break;

                    case var _ when checkedTypes.Add(type):
                        type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => !p.RESTarIgnored())
                            .Select(p => p.FieldType)
                            .ForEach(parseType);
                        type.GetDeclaredProperties().Values
                            .Where(p => !p.Hidden)
                            .Select(p => p.Type)
                            .ForEach(parseType);
                        break;
                }
            }

            var entityTypes = entityResources.Select(r => r.Type).ToList();
            var terminalTypes = terminalResources.Select(r => r.Type).ToList();

            entityTypes.ForEach(parseType);
            checkedTypes.ExceptWith(entityTypes);
            terminalTypes.ForEach(parseType);
            checkedTypes.ExceptWith(terminalTypes);

            return new Metadata
            {
                CurrentAccessScope = new Dictionary<IResource, Methods[]>(rights ?? AccessRights.Root),
                EntityResources = entityResources.ToArray(),
                TerminalResources = terminalResources.ToArray(),
                EntityResourceTypes = entityTypes
                    .Select(type => (type, type.GetDeclaredProperties().Values
                        .Cast<Member>()
                        .ToArray()))
                    .ToArray(),
                PeripheralTypes = checkedTypes
                    .Select(type =>
                    {
                        var props = type.GetDeclaredProperties().Values;
                        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => !p.RESTarIgnored())
                            .Select(f => new Field(f));
                        return (type, props.Union<Member>(fields).ToArray());
                    }).ToArray()
            };
        }

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