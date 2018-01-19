using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Auth;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using RESTar.Linq;
using static System.Reflection.BindingFlags;
using static RESTar.Methods;

namespace RESTar
{
    [RESTar(GET)]
    internal class Metadata : ISelector<Metadata>
    {
        private static readonly IEntityResource ThisResource = Resource<Metadata>.GetEntityResource;

        public AccessRights CurrentAccessRights { get; private set; }
        public List<IEntityResource> EntityResources { get; private set; }
        public List<Type> EntityResourceTypes { get; private set; }
        public List<TerminalResource> TerminalResources { get; private set; }
        public List<Type> PeripheralTypes { get; private set; }

        public IEnumerable<Metadata> Select(IRequest<Metadata> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var rights = RESTarConfig.AuthTokens[request.AuthToken];
            if (rights == null) return null;

            var entityResources = rights.Keys
                .OfType<IEntityResource>()
                .Where(r => !Equals(r, ThisResource))
                .Where(r => r.IsGlobal)
                .OrderBy(r => r.Name)
                .ToList();
            var terminalResources = rights.Keys
                .OfType<TerminalResource>()
                .ToList();
            var entityTypes = entityResources.Select(r => r.Type).ToList();
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
                        type.GetFields(Public | Instance)
                            .Where(p => !p.RESTarIgnored())
                            .Select(p => p.FieldType)
                            .ForEach(parseType);
                        type.GetDeclaredProperties().Values
                            .Where(p => p.Readable && (p.IsKey || !p.Hidden))
                            .Select(p => p.Type)
                            .ForEach(parseType);
                        break;
                }
            }

            entityTypes.ForEach(parseType);
            checkedTypes.ExceptWith(entityTypes);

            return new[]
            {
                new Metadata
                {
                    CurrentAccessRights = rights,
                    EntityResources = entityResources,
                    EntityResourceTypes = entityTypes,
                    TerminalResources = terminalResources,
                    PeripheralTypes = checkedTypes.ToList()
                }
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