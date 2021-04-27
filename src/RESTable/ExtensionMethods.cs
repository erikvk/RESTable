using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTable.ContentTypeProviders;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Meta.Internal;
using RESTable.Requests;
using RESTable.Requests.Filters;
using RESTable.Requests.Processors;
using RESTable.Resources;
using RESTable.Results;
using RESTable.Linq;
using RESTable.ProtocolProviders;
using static System.Globalization.DateTimeStyles;
using static System.Reflection.BindingFlags;
using static System.StringComparison;
using static RESTable.ErrorCodes;
using static RESTable.Requests.Operators;

namespace RESTable
{
    /// <summary>
    /// Extension methods used by RESTable
    /// </summary>
    public static class ExtensionMethods
    {
        #region Member reflection

        public static string RESTableMemberName(this MemberInfo m, bool flagged = false)
        {
            string name;
            if (m.HasAttribute<RESTableMemberAttribute>(out var attribute) && attribute.Name != null)
                name = attribute.Name;
            else name = m.Name;
            return flagged ? $"${name}" : name;
        }

        public static bool RESTableIgnored(this MemberInfo m) => m.GetCustomAttribute<RESTableMemberAttribute>()?.Ignored == true ||
                                                                 m.HasAttribute<IgnoreDataMemberAttribute>();

        #endregion

        #region Type reflection

        public static string GetRESTableTypeName(this Type type) => type?.FullName?.Replace('+', '.');

        /// <summary>
        /// Can this type hold dynamic members? Defined as implementing the IDictionary`2 interface
        /// </summary>
        public static bool IsDynamic(this Type type) => type.ImplementsGenericInterface(typeof(IDictionary<,>))
                                                        || typeof(IDynamicMemberValueProvider).IsAssignableFrom(type);

        internal static IList<Type> GetConcreteSubclasses(this Type baseType) => baseType.GetSubclasses()
            .Where(type => !type.IsAbstract)
            .ToList();

        internal static IEnumerable<Type> GetSubclasses(this Type baseType) => AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch
                {
                    return new Type[0];
                }
            })
            .Where(type => type.IsSubclassOf(baseType));

        internal static bool HasAttribute(this MemberInfo type, Type attributeType)
        {
            return (type?.GetCustomAttributes(attributeType).Any()).GetValueOrDefault();
        }

        public static bool HasResourceProviderAttribute(this Type resource)
        {
            return resource.GetCustomAttributes().OfType<EntityResourceProviderAttribute>().Any();
        }

        public static bool HasAttribute<TAttribute>(this MemberInfo type) where TAttribute : Attribute
        {
            return type?.GetCustomAttribute<TAttribute>() != null;
        }

        public static bool HasAttribute<TAttribute>(this MemberInfo type, out TAttribute attribute) where TAttribute : Attribute
        {
            attribute = type?.GetCustomAttribute<TAttribute>();
            return attribute != null;
        }

        internal static IEnumerable<Type> GetBaseTypes(this Type type)
        {
            var currentBaseType = type.BaseType;
            while (currentBaseType != null)
            {
                yield return currentBaseType;
                currentBaseType = currentBaseType.BaseType;
            }
        }

        internal static Type[] GetGenericTypeParameters(this Type type, Type typeDef = null)
        {
            var toMatch = typeDef == null ? type : type?.GetBaseTypes().FirstOrDefault(t => t.GetGenericTypeDefinition() == typeDef);
            return toMatch?.IsGenericType != true ? null : toMatch.GenericTypeArguments;
        }

        /// <summary>
        /// Returns true if and only if the type implements the generic interface type
        /// </summary>
        public static bool ImplementsGenericInterface(this Type type, Type interfaceType)
        {
            if (type.Name == interfaceType.Name &&
                type.Namespace == interfaceType.Namespace &&
                type.Assembly == interfaceType.Assembly)
                return true;
            return type
                .GetInterfaces()
                .Any(i => i.Name == interfaceType.Name &&
                          i.Namespace == interfaceType.Namespace &&
                          i.Assembly == interfaceType.Assembly);
        }

        /// <summary>
        /// Returns true if and only if the type implements the generic interface type. Returns the 
        /// generic type parameters (if any) in an out parameter.
        /// </summary>
        public static bool ImplementsGenericInterface(this Type type, Type interfaceType, out Type[] genericParameters)
        {
            var match = type.GetInterfaces()
                .FirstOrDefault(i => i.Name == interfaceType.Name &&
                                     i.Namespace == interfaceType.Namespace &&
                                     i.Assembly == interfaceType.Assembly);
            if (match == null &&
                type.Name == interfaceType.Name &&
                type.Namespace == interfaceType.Namespace &&
                type.Assembly == interfaceType.Assembly)
                match = type;
            genericParameters = match?.GetGenericArguments();
            return match != null;
        }

        internal static long ByteCount(this PropertyInfo property, object target)
        {
            if (target == null) throw new NullReferenceException(nameof(target));
            return property.GetValue(target) switch
            {
                null => 0,
                string str => Encoding.UTF8.GetByteCount(str),
                byte[] binary => binary.Length,
                _ => CountBytes(property.PropertyType)
            };
        }

        internal static long CountBytes(this Type type)
        {
            if (type.IsEnum) return 8;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    if (type.IsNullable(out var baseType))
                        return CountBytes(baseType);
                    throw new Exception($"Unknown type encountered: '{type.GetRESTableTypeName()}'");
                case TypeCode.Boolean: return 4;
                case TypeCode.Char: return 2;
                case TypeCode.SByte: return 1;
                case TypeCode.Byte: return 1;
                case TypeCode.Int16: return 2;
                case TypeCode.UInt16: return 2;
                case TypeCode.Int32: return 4;
                case TypeCode.UInt32: return 4;
                case TypeCode.Int64: return 8;
                case TypeCode.UInt64: return 8;
                case TypeCode.Single: return 4;
                case TypeCode.Double: return 8;
                case TypeCode.Decimal: return 16;
                case TypeCode.DateTime: return 8;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region Other

        internal static string UriEncode(this string str)
        {
            if (str == null) return null;
            return Uri.EscapeDataString(str);
        }

        internal static string UriDecode(this string str)
        {
            if (str == null) return null;
            return Uri.UnescapeDataString(str);
        }

        internal static bool EqualsNoCase(this string s1, string s2) => string.Equals(s1, s2, OrdinalIgnoreCase);
        internal static string ToMethodsString(this IEnumerable<Method> ie) => string.Join(", ", ie);

        internal static object GetDefault(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return DEFAULT_METHOD.MakeGenericMethod(type).Invoke(null, null);
        }

        private static readonly MethodInfo DEFAULT_METHOD = typeof(ExtensionMethods)
            .GetMethod(nameof(DEFAULT), NonPublic | Static);

        private static object DEFAULT<T>() => default(T);

        public static string Fnuttify(this string sqlKey) => $"\"{sqlKey.Replace(".", "\".\"")}\"";

        /// <summary>
        /// Checks if the type is a Nullable struct, and - if so -  returns the underlying type in the out parameter.
        /// </summary>
        public static bool IsNullable(this Type type, out Type baseType)
        {
            baseType = Nullable.GetUnderlyingType(type);
            return baseType != null;
        }

        internal static (T, T) Deconstruct<T>(this ICollection<T> collection)
        {
            if (collection.Count > 2) throw new InvalidOperationException("Collection contained more than two elements");
            return (collection.ElementAtOrDefault(0), collection.ElementAtOrDefault(1));
        }

        /// <summary>
        /// Splits a string by a separator string
        /// </summary>
        public static string[] Split(this string str, string separator, StringSplitOptions options = StringSplitOptions.None)
        {
            return str.Split(new[] {separator}, options);
        }

        /// <summary>
        /// Splits a string into two parts by a separator char, and returns a 2-tuple 
        /// holding the parts.
        /// </summary>
        public static (string, string) TSplit(this string str, char separator, bool trim = false)
        {
            var split = str.Split(new[] {separator}, 2, StringSplitOptions.RemoveEmptyEntries);
            return trim switch
            {
                false => split.Length switch
                {
                    1 => (split[0], null),
                    2 => (split[0], split[1]),
                    _ => throw new ArgumentOutOfRangeException()
                },
                true => split.Length switch
                {
                    1 => (split[0].Trim(), null),
                    2 => (split[0].Trim(), split[1].Trim()),
                    _ => throw new ArgumentOutOfRangeException()
                }
            };
        }

        /// <summary>
        /// Splits a string into two parts by a separator string, and returns a 2-tuple 
        /// holding the parts.
        /// </summary>
        public static (string, string) TSplit(this string str, string separator, bool trim = false)
        {
            var split = str.Split(new[] {separator}, 2, StringSplitOptions.None);
            return trim switch
            {
                false => split.Length switch
                {
                    1 => (split[0], null),
                    2 => (split[0], split[1]),
                    _ => throw new ArgumentOutOfRangeException()
                },
                true => split.Length switch
                {
                    1 => (split[0].Trim(), null),
                    2 => (split[0].Trim(), split[1].Trim()),
                    _ => throw new ArgumentOutOfRangeException()
                }
            };
        }

        #endregion

        #region Resource helpers

        internal static ResourceKind GetResourceKind(this Type metatype) => metatype switch
        {
            _ when typeof(IEntityResource).IsAssignableFrom(metatype) => ResourceKind.EntityResource,
            _ when typeof(ITerminalResource).IsAssignableFrom(metatype) => ResourceKind.TerminalResource,
            _ when typeof(IBinaryResource).IsAssignableFrom(metatype) => ResourceKind.BinaryResource,
            _ when typeof(IEventResource).IsAssignableFrom(metatype) => ResourceKind.EventResource,
            _ => ResourceKind.All
        };

        internal static (bool allowDynamic, TermBindingRule bindingRule) GetDynamicConditionHandling(this Type type, RESTableAttribute attribute)
        {
            var dynamicConditionsAllowed = typeof(IDynamicMemberValueProvider).IsAssignableFrom(type) ||
                                           attribute?.AllowDynamicConditions == true;
            var conditionBindingRule = dynamicConditionsAllowed ? TermBindingRule.DeclaredWithDynamicFallback : TermBindingRule.OnlyDeclared;
            return (dynamicConditionsAllowed, conditionBindingRule);
        }

        internal static Type GetRESTableInterfaceType(this Type resourceType) => resourceType
            .GetInterfaces()
            .FirstOrDefault(i => typeof(IEntityDefinition).IsAssignableFrom(i));

        internal static IEnumerable<Operator> ToOperators(this Operators operators)
        {
            var opList = new List<Operator>();
            if (operators.HasFlag(EQUALS)) opList.Add(EQUALS);
            if (operators.HasFlag(NOT_EQUALS)) opList.Add(NOT_EQUALS);
            if (operators.HasFlag(LESS_THAN)) opList.Add(LESS_THAN);
            if (operators.HasFlag(GREATER_THAN)) opList.Add(GREATER_THAN);
            if (operators.HasFlag(LESS_THAN_OR_EQUALS)) opList.Add(LESS_THAN_OR_EQUALS);
            if (operators.HasFlag(GREATER_THAN_OR_EQUALS)) opList.Add(GREATER_THAN_OR_EQUALS);
            return opList;
        }

        /// <summary>
        /// Converts a resource entitiy to a JSON.net JObject.
        /// </summary>
        internal static JObject ToJObject(this object entity)
        {
            var jsonProvider = ApplicationServicesAccessor.JsonProvider;

            switch (entity)
            {
                case JObject j: return j;
                case Dictionary<string, object> _idict: return _idict.ToJObject();
                case IDictionary idict:
                    var _jobj = new JObject();
                    foreach (DictionaryEntry pair in idict)
                        _jobj[pair.Key.ToString()] = pair.Value == null
                            ? null
                            : JToken.FromObject(pair.Value, jsonProvider.GetSerializer());
                    return _jobj;
            }

            var jobj = new JObject();
            var typeCache = ApplicationServicesAccessor.TypeCache;
            foreach (var property in typeCache.GetDeclaredProperties(entity.GetType()).Values.Where(p => !p.Hidden))
            {
                var propertyValue = property.GetValue(entity);
                jobj[property.Name] = propertyValue == null ? null : JToken.FromObject(propertyValue, jsonProvider.GetSerializer());
            }
            return jobj;
        }

        internal static string GetEntityResourceProviderId(this Type providerType)
        {
            var typeName = providerType.Name;
            if (typeName == null) throw new ArgumentNullException();
            if (typeName.EndsWith("resourceprovider", InvariantCultureIgnoreCase))
                typeName = typeName.Substring(0, typeName.Length - 16);
            else if (typeName.EndsWith("provider", InvariantCultureIgnoreCase))
                typeName = typeName.Substring(0, typeName.Length - 8);
            return typeName;
        }

        public static Type GetWrappedType(this Type wrapperType) => wrapperType.BaseType?.GetGenericArguments()[0];

        public static bool IsWrapper(this Type type) => typeof(IResourceWrapper).IsAssignableFrom(type);

        #endregion

        #region Filter and Process

        internal static IAsyncEnumerable<T> Filter<T>(this IAsyncEnumerable<T> entities, IFilter filter) where T : class
        {
            return filter?.Apply(entities) ?? entities;
        }

        internal static IAsyncEnumerable<JObject> Process<T>(this IAsyncEnumerable<T> entities, params IProcessor[] processors)
            where T : class
        {
            return processors.Aggregate(default(IAsyncEnumerable<JObject>), (e, p) => e != null ? p.Apply(e) : p.Apply(entities));
        }

        #endregion

        #region NETSTANDARD2.0

#if NETSTANDARD2_0
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable)
        {
            return new(enumerable);
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable, IEqualityComparer<T> equalityComparer)
        {
            return new(enumerable, equalityComparer);
        }
#endif

        #endregion

        #region Dictionary helpers

        /// <summary>
        /// Gets the value of a key from an IDictionary, or null if the dictionary does not contain the key.
        /// </summary>
        public static TValue SafeGet<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            dict.TryGetValue(key, out var value);
            return value;
        }

        /// <summary>
        /// Adds the tuple to the IDictionary
        /// </summary>
        public static void TAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, (TKey key, TValue value) pair)
        {
            dict.Add(pair.key, pair.value);
        }

        /// <summary>
        /// Puts the tuple into the IDictionary
        /// </summary>
        public static void TPut<TKey, TValue>(this IDictionary<TKey, TValue> dict, (TKey key, TValue value) pair)
        {
            dict[pair.key] = pair.value;
        }

        /// <summary>
        /// Puts the KeyValuePair into the IDictionary
        /// </summary>
        public static void Put<TKey, TValue>(this IDictionary<TKey, TValue> dict, KeyValuePair<TKey, TValue> pair)
        {
            dict[pair.Key] = pair.Value;
        }

        internal static Dictionary<TKey, T> SafeToDictionary<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector,
            IEqualityComparer<TKey> equalityComparer)
        {
            var dictionary = new Dictionary<TKey, T>(equalityComparer);
            foreach (var item in source)
                dictionary[keySelector(item)] = item;
            return dictionary;
        }

        /// <summary>
        /// Gets the value of a key from an IDictionary, without case sensitivity, or null if the dictionary does 
        /// not contain the key. The actual key is returned in the actualKey out parameter.
        /// </summary>
        internal static bool TryFindInDictionary<T>(this IDictionary<string, T> dict, string key, out string actualKey,
            out T result)
        {
            result = default;
            actualKey = null;
            var matches = dict.Where(pair => pair.Key.EqualsNoCase(key)).ToList();
            switch (matches.Count)
            {
                case 0: return false;
                case 1:
                    actualKey = matches[0].Key;
                    result = matches[0].Value;
                    return true;
                default:
                    if (!dict.TryGetValue(key, out result)) return false;
                    actualKey = key;
                    return true;
            }
        }

        internal static string Capitalize(this string input)
        {
            var array = input.ToCharArray();
            array[0] = char.ToUpper(array[0]);
            return new string(array);
        }

        /// <summary>
        /// Converts a Dictionary object to a JSON.net JObject
        /// </summary>
        public static JObject ToJObject(this Dictionary<string, object> dictionary)
        {
            var jobj = new JObject();
            foreach (var (key, value) in dictionary)
                jobj[key] = MakeJToken(value);
            return jobj;
        }

        private static JToken MakeJToken(dynamic value)
        {
            try
            {
                return (JToken) value;
            }
            catch
            {
                try
                {
                    return new JArray(value);
                }
                catch
                {
                    return JToken.FromObject(value);
                }
            }
        }

        private static IEnumerable<DictionaryEntry> Cast(this IDictionary dict) => dict.Cast<DictionaryEntry>();

        #endregion

        #region Requests

        private static readonly CultureInfo en_US = new("en-US");

        internal static string GetFriendlyTypeName(this Type type) => Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => "a boolean (true or false)",
            TypeCode.Char => "a single character",
            TypeCode.SByte => $"an integer ({sbyte.MinValue} to {sbyte.MaxValue})",
            TypeCode.Byte => $"a positive integer ({byte.MinValue} to {byte.MaxValue})",
            TypeCode.Int16 => $"an integer ({short.MinValue} to {short.MaxValue})",
            TypeCode.UInt16 => $"a positive integer ({ushort.MinValue} to {ushort.MaxValue})",
            TypeCode.Int32 => "an integer (32-bit)",
            TypeCode.Int64 => "an integer (64-bit)",
            TypeCode.UInt32 => "a positive integer (32-bit)",
            TypeCode.UInt64 => "a positive integer (64-bit)",
            TypeCode.Single => "a floating point number (single)",
            TypeCode.Double => "a floating point number (double)",
            TypeCode.Decimal => "a floating point number",
            TypeCode.String => "a string",
            TypeCode.DateTime => "a date time",
            _ when type.IsNullable(out var baseType) => baseType.GetFriendlyTypeName(),
            _ => type.FullName
        };

        public static Error AsError(this Exception exception) => exception switch
        {
            Error re => re,
            FormatException _ => new UnsupportedContent(exception),
            JsonReaderException jre => new FailedJsonDeserialization(jre),
            RuntimeBinderException _ => new BinderPermissions(exception),
            ArgumentException _ => new BadRequest(ErrorCodes.Unknown, exception.Message, exception),
            NotImplementedException _ => new FeatureNotImplemented("RESTable encountered a call to a non-implemented method"),
            _ => new Unknown(exception)
        };

        public static Error AsResultOf(this Exception exception, IRequest request)
        {
            var error = exception.AsError();
            if (request == null) return error;
            error.SetContext(request.Context);
            error.Request = request;
            if (error is not Forbidden && request.Method >= 0)
            {
                var errorId = Admin.Error.Create(error, request).Id;
                error.Headers.Error = $"/restable.admin.error/id={errorId}";
            }
            if (request.Headers.Metadata.EqualsNoCase("full"))
                error.Headers.Metadata = error.Metadata;
            error.Headers.Version = request.GetRequiredService<RESTableConfiguration>().Version;
            return error;
        }

        /// <summary>
        /// Creates a new writeable UriComponents instance from a possibly read-only IUriComponents instance
        /// </summary>
        public static Requests.UriComponents ToWritable(this IUriComponents components)
        {
            return components as Requests.UriComponents ?? new Requests.UriComponents(components);
        }

        /// <summary>
        /// Generates new UriComponents that encode a request for the next page of entities, calculated from an IEntities entity collection.
        /// The count parameter controls the size of the next page. If omitted, the size is the same as the current page.
        /// </summary>
        public static IUriComponents GetNextPageLink(this IEntities entities, long entityCount, int nextPageSize)
        {
            var components = entities.Request.UriComponents.ToWritable();
            if (nextPageSize > -1)
            {
                components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase(nameof(Limit)));
                components.MetaConditions.Add(new UriCondition(RESTableMetaCondition.Limit, nextPageSize.ToString()));
            }
            components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase(nameof(Offset)));
            var previousOffset = entities.Request.MetaConditions.Offset;
            var offsetNr = entityCount + previousOffset.Number;
            UriCondition offset;
            if (previousOffset == -1)
                offset = new UriCondition(RESTableMetaCondition.Offset, "∞");
            else if (previousOffset < -1 && offsetNr >= -1)
                offset = new UriCondition(RESTableMetaCondition.Offset, (-1).ToString());
            else offset = new UriCondition(RESTableMetaCondition.Offset, offsetNr.ToString());
            components.MetaConditions.Add(offset);
            return components;
        }

        /// <summary>
        /// Generates new UriComponents that encode a request for the previous page of entities, calculated from an IEntities entity collection.
        /// The count parameter controls the size of the next page. If omitted, the size is the same as the current page.
        /// </summary>
        public static IUriComponents GetPreviousPageLink(this IEntities entities, long entityCount, int nextPageSize = -1)
        {
            var components = entities.Request.UriComponents.ToWritable();
            var previousOffset = entities.Request.MetaConditions.Offset;
            var pageSize = entityCount;
            if (nextPageSize > -1)
            {
                components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase(nameof(Limit)));
                components.MetaConditions.Add(new UriCondition(RESTableMetaCondition.Limit, nextPageSize.ToString()));
                pageSize = nextPageSize;
            }
            var offsetNr = previousOffset.Number - pageSize;
            components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase(nameof(Offset)));
            UriCondition offset;
            if (previousOffset == 0)
                offset = new UriCondition(RESTableMetaCondition.Offset, "-∞");
            else if (previousOffset > 0 && offsetNr <= 0)
            {
                if (long.TryParse(components.MetaConditions.FirstOrDefault(c => c.Key.EqualsNoCase(nameof(Limit)))?.ValueLiteral, out var limit))
                {
                    components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase(nameof(Limit)));
                    components.MetaConditions.Add(new UriCondition(RESTableMetaCondition.Limit, (limit + offsetNr).ToString()));
                }
                offset = new UriCondition(RESTableMetaCondition.Offset, 0.ToString());
            }
            else offset = new UriCondition(RESTableMetaCondition.Offset, offsetNr.ToString());
            components.MetaConditions.Add(offset);
            return components;
        }

        /// <summary>
        /// Generates new UriComponents that encode a request for the first number of entities, calculated from an IEntities entity collection.
        /// The count parameter controls how many entities are selected. If omitted, one entity is selected.
        /// </summary>
        public static IUriComponents GetFirstLink(this IEntities entities, int count = 1)
        {
            var components = entities.Request.UriComponents.ToWritable();
            components.MetaConditions.RemoveAll(m => m.Key.EqualsNoCase(nameof(Offset)) || m.Key.EqualsNoCase(nameof(Limit)));
            components.MetaConditions.Add(new UriCondition(RESTableMetaCondition.Limit, count.ToString()));
            return components;
        }

        /// <summary>
        /// Generates new UriComponents that encode a request for the last number of entities, calculated from an IEntities entity collection.
        /// The count parameter controls how many entities are selected. If omitted, one entity is selected.
        /// </summary>
        public static IUriComponents GetLastLink(this IEntities entities, int count = 1)
        {
            var components = entities.Request.UriComponents.ToWritable();
            components.MetaConditions.RemoveAll(m => m.Key.EqualsNoCase(nameof(Offset)) || m.Key.EqualsNoCase(nameof(Limit)));
            components.MetaConditions.Add(new UriCondition(RESTableMetaCondition.Offset, (-count).ToString()));
            components.MetaConditions.Add(new UriCondition(RESTableMetaCondition.Limit, count.ToString()));
            return components;
        }

        /// <summary>
        /// Generates new UriComponents that encode a request for all entities in a resource, calculated from an IEntities entity collection.
        /// </summary>
        public static IUriComponents GetAllLink(this IEntities entities)
        {
            var components = entities.Request.UriComponents.ToWritable();
            components.MetaConditions.RemoveAll(m => m.Key.EqualsNoCase(nameof(Offset)) || m.Key.EqualsNoCase(nameof(Limit)));
            return components;
        }

        public static IContentTypeProvider GetInputContentTypeProvider(this IProtocolHolder protocolHolder, ContentType? contentTypeOverride = null)
        {
            var contentType = contentTypeOverride ?? protocolHolder.Headers.ContentType ?? protocolHolder.CachedProtocolProvider.DefaultInputProvider.ContentType;
            if (!protocolHolder.CachedProtocolProvider.InputMimeBindings.TryGetValue(contentType.MediaType, out var contentTypeProvider))
                throw new UnsupportedContent(contentType.ToString());
            return contentTypeProvider;
        }

        /// <summary>
        /// Checks if the given protocol holder can accept a request with the given content type
        /// </summary>
        public static bool Accepts(this IProtocolHolder protocolHolder, ContentType contentType, out string acceptHeader)
        {
            acceptHeader = protocolHolder.Headers.Accept.ToString();
            foreach (var acceptType in protocolHolder.Headers.Accept)
            {
                if (acceptType.AnyType) return true;
                if (protocolHolder.CachedProtocolProvider.OutputMimeBindings.TryGetValue(acceptType.MediaType, out _))
                    return true;
            }
            return false;
        }

        public static IContentTypeProvider GetOutputContentTypeProvider(this IProtocolHolder protocolHolder, ContentType? contentTypeOverride = null)
        {
            IContentTypeProvider acceptProvider = null;

            var protocolProvider = protocolHolder.CachedProtocolProvider;
            var headers = protocolHolder.Headers;
            var contentType = contentTypeOverride;
            if (contentType.HasValue)
                contentType = contentType.Value;
            else if (!(headers.Accept?.Count > 0))
                contentType = protocolProvider.DefaultOutputProvider.ContentType;
            if (!contentType.HasValue)
            {
                var containedWildcard = false;
                var foundProvider = headers.Accept.Any(a =>
                {
                    if (!a.AnyType)
                        return protocolProvider.OutputMimeBindings.TryGetValue(a.MediaType, out acceptProvider);
                    containedWildcard = true;
                    return false;
                });
                if (!foundProvider)
                    if (containedWildcard)
                        acceptProvider = protocolProvider.DefaultOutputProvider;
                    else
                        throw new NotAcceptable(headers.Accept.ToString());
            }
            else if (!protocolProvider.OutputMimeBindings.TryGetValue(contentType.Value.MediaType, out acceptProvider))
                throw new NotAcceptable(contentType.Value.ToString());
            return acceptProvider;
        }

        /// <summary>
        /// Parses a condition value from a value literal, and performs an optional type check (non-optional for enums)
        /// </summary>
        internal static object ParseConditionValue(this string valueLiteral, DeclaredProperty property = null)
        {
            switch (valueLiteral)
            {
                case null:
                case "null": return null;
                case "": return "";
            }

            if (property is DeclaredProperty {IsEnum: true} prop)
            {
                try
                {
                    if (prop.IsNullable)
                    {
                        var type = Nullable.GetUnderlyingType(prop.Type) ?? prop.Type;
                        return Enum.Parse(type, valueLiteral, true);
                    }
                    return Enum.Parse(property.Type, valueLiteral, true);
                }
                catch
                {
                    throw new InvalidSyntax(InvalidEnumValue, $"'{valueLiteral}' is not a valid enum value for property '{property.Name}'");
                }
            }
            var (first, length) = (valueLiteral[0], valueLiteral.Length);
            var escapedString = false;
            switch (first)
            {
                case '\'':
                case '\"':
                    if (length > 1 && valueLiteral[length - 1] == first)
                    {
                        valueLiteral = valueLiteral.Substring(1, length - 2);
                        escapedString = true;
                    }
                    break;
            }
            if (property != null)
            {
                try
                {
                    if (property.IsDateTime)
                    {
                        if (DateTime.TryParse(valueLiteral, null, AssumeUniversal, out var dateTime))
                            return dateTime.ToUniversalTime();
                        throw new Exception();
                    }
                    return Convert.ChangeType(valueLiteral, property.Type.IsNullable(out var t) ? t : property.Type);
                }
                catch
                {
                    throw new InvalidConditionValueType(valueLiteral, property);
                }
            }
            if (escapedString) return valueLiteral;
            switch (valueLiteral)
            {
                case "false":
                case "False":
                case "FALSE": return false;
                case "true":
                case "True":
                case "TRUE": return true;
            }
            if (char.IsDigit(first))
            {
                if (int.TryParse(valueLiteral, NumberStyles.Any, CultureInfo.InvariantCulture, out var i))
                    return i;
                if (decimal.TryParse(valueLiteral, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                    return d;
                if (DateTime.TryParse(valueLiteral, null, AssumeUniversal, out var dateTime))
                    return dateTime.ToUniversalTime();
            }
            return valueLiteral;
        }

        /// <summary>
        /// Returns true if and only if the request contains a condition with the given key and 
        /// operator (case insensitive). If true, the out Condition parameter will contain a reference to the found
        /// condition.
        /// </summary>
        public static bool TryGetCondition<T>(this IRequest<T> request, string key, Operators op, out Condition<T> condition) where T : class
        {
            condition = request.Conditions?.Get(key, op);
            return condition != null;
        }

        /// <summary>
        /// Returns true if and only if the request contains at least one condition with the given key (case insensitive). 
        /// If true, the out Conditions parameter will contain all the matching conditions
        /// </summary>
        /// <returns></returns>
        public static bool TryGetConditions<T>(this IRequest<T> request, string key, out ICollection<Condition<T>> conditions) where T : class
        {
            conditions = request.Conditions.Get(key).ToList();
            return !conditions.Any() != true;
        }

        #endregion

        #region Conversion

        internal static double GetRESTableElapsedMs(this TimeSpan timeSpan)
        {
            return Math.Round(timeSpan.TotalMilliseconds, 4);
        }

        internal static string ToStringRESTable(this TimeSpan timeSpan)
        {
            return timeSpan.GetRESTableElapsedMs().ToString(CultureInfo.InvariantCulture);
        }

        internal static string SHA256(this string input)
        {
            using var hasher = System.Security.Cryptography.SHA256.Create();
            return Convert.ToBase64String(hasher.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        internal static byte[] ToBytes(this string str)
        {
            return str != null ? Encoding.UTF8.GetBytes(str) : null;
        }

        internal static byte[] ToByteArray(this Stream stream)
        {
            switch (stream)
            {
                case null: return null;
                case MemoryStream ms: return ms.ToArray();
                case Body body: return body.GetBytes();
                default:
                {
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    if (stream.CanSeek)
                        stream.Seek(0, SeekOrigin.Begin);
                    return ms.ToArray();
                }
            }
        }

        internal static async Task<byte[]> ToByteArrayAsync(this Stream stream)
        {
            switch (stream)
            {
                case null: return null;
                case MemoryStream ms: return ms.ToArray();
                default:
                {
                    var ms = new MemoryStream();
#if NETSTANDARD2_1
                    await using (ms)
#else
                    using (ms)
#endif
                    {
                        await stream.CopyToAsync(ms).ConfigureAwait(false);
                        if (stream.CanSeek)
                            stream.Seek(0, SeekOrigin.Begin);
                        return ms.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Converts a boolean into an XML boolean string, i.e. "true" or "false" 
        /// </summary>
        /// <param name="bool"></param>
        /// <returns></returns>
        public static string XMLBool(this bool @bool)
        {
            const string trueString = "true";
            const string falseString = "false";
            if (@bool)
                return trueString;
            return falseString;
        }

        /// <summary>
        /// Converts an HTTP status code to the underlying numeric code
        /// </summary>
        internal static ushort? ToCode(this HttpStatusCode statusCode) => (ushort) statusCode;

        /// <summary>
        /// Creates a formatted string representation of the URI components,
        /// a valid URI string according to the assigned protocol.
        /// </summary>
        public static string ToUriString(this IUriComponents uriComponents)
        {
            var protocolProvider = uriComponents.ProtocolProvider;
            var uriString = protocolProvider.MakeRelativeUri(uriComponents);
            if (protocolProvider is DefaultProtocolProvider)
                return uriString;
            return $"-{protocolProvider.ProtocolIdentifier}{uriString}";
        }

        internal static string ReplaceFirst(this string text, string search, string replace, out bool replaced)
        {
            var pos = text.IndexOf(search, Ordinal);
            if (pos < 0)
            {
                replaced = false;
                return text;
            }

            replaced = true;
            return $"{text.Substring(0, pos)}{replace}{text.Substring(pos + search.Length)}";
        }

        internal static Method[] ToMethodsArray(this string methodsString)
        {
            if (methodsString == null) return null;
            if (methodsString.Trim() == "*")
                return EnumMember<Method>.Values;
            return methodsString.Split(',')
                .Where(s => s != "")
                .Select(s => (Method) Enum.Parse(typeof(Method), s))
                .ToArray();
        }

        #endregion
    }
}