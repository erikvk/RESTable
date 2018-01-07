using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Auth;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using RESTar.Linq;
using static RESTar.Methods;

namespace RESTar
{
    [RESTar(GET)]
    internal class Metadata : ISelector<Metadata>
    {
        public AccessRights CurrentAccessRights { get; private set; }
        public List<Type> EnumTypes { get; private set; }
        public List<Type> EntityTypes { get; private set; }
        public List<IResource> EntitySets { get; private set; }

        public IEnumerable<Metadata> Select(IRequest<Metadata> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var rights = RESTarConfig.AuthTokens[request.AuthToken];
            var enumTypes = new HashSet<Type>();
            var entityTypes = new HashSet<Type>();
            var thisResource = Resource<Metadata>.Get;
            var resources = rights?.Keys
                .Where(r => r.IsGlobal && !r.IsInnerResource)
                .Where(r => rights.ContainsKey(r))
                .Where(r => r != thisResource)
                .OrderBy(r => r.FullName)
                .ToList();
            if (rights == null) return null;

            void parseType(Type type)
            {
                if (type == typeof(object))
                    return;
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                    return;
                if (type.IsEnum)
                {
                    enumTypes.Add(type);
                    return;
                }
                if (type.IsNullable(out var t))
                {
                    parseType(t);
                    return;
                }
                if (type.Implements(typeof(IEnumerable<>), out var param))
                {
                    if (param[0].Implements(typeof(IEnumerable<>)))
                        return;
                    parseType(param[0]);
                    return;
                }
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
                    case TypeCode.String: return;
                }
                if (entityTypes.Add(type))
                {
                    type.GetDeclaredProperties()
                        .Select(pair => pair.Value.Type)
                        .ForEach(parseType);
                }
            }

            resources.Select(r => r.Type).ForEach(parseType);

            return new[]
            {
                new Metadata
                {
                    CurrentAccessRights = rights,
                    EnumTypes = enumTypes.ToList(),
                    EntityTypes = entityTypes.ToList(),
                    EntitySets = resources
                }
            };
        }
    }
}