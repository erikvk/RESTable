using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Dynamit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Auth;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Resources;
using RESTar.Serialization;
using RESTar.View;
using Starcounter;
using static System.Globalization.DateTimeStyles;
using static System.Globalization.NumberStyles;
using static System.Reflection.BindingFlags;
using static System.StringComparison;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Operators;
using static RESTar.Requests.Responses;
using static Starcounter.DbHelper;
using IResource = RESTar.Internal.IResource;


namespace RESTar
{
    /// <summary>
    /// Extension methods used by RESTar
    /// </summary>
    public static class ExtensionMethods
    {
        #region Member reflection

        internal static string RESTarMemberName(this MemberInfo m, bool flagged = false)
        {
            var name = m.GetAttribute<RESTarMemberAttribute>()?.Name ??
                       m.GetAttribute<DataMemberAttribute>()?.Name ??
                       m.GetAttribute<JsonPropertyAttribute>()?.PropertyName ??
                       m.Name;
            return flagged ? "$" + name : name;
        }

        internal static bool RESTarIgnored(this MemberInfo m) => m.GetAttribute<RESTarMemberAttribute>()?.Ignored == true ||
                                                                 m.HasAttribute<IgnoreDataMemberAttribute>();

        #endregion

        #region Type reflection

        internal static bool IsDynamic(this Type type) => type.Implements(typeof(IDictionary<,>));

        internal static bool IsDDictionary(this Type type) => type == typeof(DDictionary) ||
                                                              type.IsSubclassOf(typeof(DDictionary));

        internal static bool IsStarcounter(this Type type) => type.HasAttribute<DatabaseAttribute>();

        internal static IList<Type> GetConcreteSubclasses(this Type baseType) => baseType.GetSubclasses()
            .Where(type => !type.IsAbstract)
            .ToList();

        internal static IEnumerable<Type> GetSubclasses(this Type baseType) =>
            from assembly in AppDomain.CurrentDomain.GetAssemblies()
            from type in assembly.GetTypes()
            where type.IsSubclassOf(baseType)
            select type;

        internal static TAttribute GetAttribute<TAttribute>(this MemberInfo type) where TAttribute : Attribute =>
            type?.GetCustomAttributes<TAttribute>().FirstOrDefault();

        internal static bool HasAttribute(this MemberInfo type, Type attributeType) =>
            (type?.GetCustomAttributes(attributeType).Any()).GetValueOrDefault();

        internal static bool HasResourceProviderAttribute(this Type resource) =>
            resource.GetCustomAttributes().OfType<ResourceProviderAttribute>().Any();

        internal static bool HasAttribute<TAttribute>(this MemberInfo type)
            where TAttribute : Attribute => (type?.GetCustomAttributes<TAttribute>().Any()).GetValueOrDefault();

        internal static bool HasAttribute<TAttribute>(this MemberInfo type, out TAttribute attribute)
            where TAttribute : Attribute
        {
            attribute = type?.GetCustomAttributes<TAttribute>().FirstOrDefault();
            return attribute != null;
        }

        internal static bool Implements(this Type type, Type interfaceType) => type
            .GetInterfaces()
            .Any(i => i.Name == interfaceType.Name && i.Namespace == interfaceType.Namespace);

        internal static bool Implements(this Type type, Type interfaceType, out Type[] genericParameters)
        {
            var match = type
                .GetInterfaces()
                .FirstOrDefault(i => i.Name == interfaceType.Name && i.Namespace == interfaceType.Namespace);
            genericParameters = match?.GetGenericArguments();
            return match != null;
        }

        internal static long ByteCount(this PropertyInfo property, object target)
        {
            if (target == null) throw new NullReferenceException(nameof(target));
            switch (property.GetValue(target))
            {
                case null: return 0;
                case string str: return Encoding.UTF8.GetByteCount(str);
                case Binary binary: return binary.ToArray().Length;
                default: return CountBytes(property.PropertyType);
            }
        }

        internal static long CountBytes(this Type type)
        {
            if (type.IsEnum) return 8;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    if (type.IsNullable(out var baseType)) return CountBytes(baseType);
                    if (type.IsStarcounter()) return 16;
                    throw new Exception($"Unknown type encountered: '{type.FullName}'");
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

        /// <summary>
        /// Gets the object for a Starcounter object number
        /// </summary>
        /// <param name="objectNo">The Starcounter ObjectNo to get the extension for</param>
        /// <returns>The object with the specified ObjectNo</returns>
        public static T GetReference<T>(this ulong? objectNo) where T : class => FromID(objectNo ?? 0) as T;

        /// <summary>
        /// Gets the object for a Starcounter object number
        /// </summary>
        /// <param name="objectNo">The Starcounter ObjectNo to get the extension for</param>
        /// <returns>The object with the specified ObjectNo</returns>
        public static T GetReference<T>(this ulong objectNo) where T : class => FromID(objectNo) as T;

        internal static bool EqualsNoCase(this string s1, string s2) => string.Equals(s1, s2, CurrentCultureIgnoreCase);
        internal static string ToMethodsString(this IEnumerable<Methods> ie) => string.Join(", ", ie);

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

        internal static Methods[] ToMethodsArray(this string methodsString)
        {
            if (methodsString == null) return null;
            if (methodsString.Trim() == "*")
                return RESTarConfig.Methods;
            return methodsString.Split(',')
                .Where(s => s != "")
                .Select(s => (Methods) Enum.Parse(typeof(Methods), s))
                .ToArray();
        }

        internal static object GetDefault(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return DEFAULT_METHOD.MakeGenericMethod(type).Invoke(null, null);
        }

        private static readonly MethodInfo DEFAULT_METHOD = typeof(ExtensionMethods)
            .GetMethod(nameof(DEFAULT), NonPublic | Static);

        private static object DEFAULT<T>() => default(T);

        internal static AccessRights ToAccessRights(this List<AccessRight> accessRights)
        {
            var ar = new AccessRights();
            foreach (var right in accessRights)
            foreach (var resource in right.Resources)
                ar[resource] = ar.ContainsKey(resource)
                    ? ar[resource].Union(right.AllowedMethods).ToArray()
                    : right.AllowedMethods;
            return ar;
        }

        internal static string Fnuttify(this string sqlKey) => $"\"{sqlKey.Replace(".", "\".\"")}\"";

        internal static bool IsNullable(this Type type, out Type baseType)
        {
            baseType = null;
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Nullable<>))
                return false;
            baseType = type.GenericTypeArguments[0];
            return true;
        }

        /// <summary>
        /// Tries to get the target T2 by executing the selector method on the T1 object. If the selector 
        /// executes successfully, returns the target T2. Else return the default for T2.
        /// </summary>
        internal static T2 SafeGet<T1, T2>(this T1 obj, Func<T1, T2> selector)
        {
            try
            {
                return selector(obj);
            }
            catch
            {
                return default;
            }
        }

        internal static (string, string) TSplit(this string str, char separator)
        {
            var split = str.Split(separator);
            return (split[0], split[1]);
        }

        internal static (string, string) TSplit(this string str, string separator)
        {
            var split = str.Split(new[] {separator}, StringSplitOptions.None);
            return (split[0], split[1]);
        }

        #endregion

        #region Resource helpers

        internal static void Validate(this IValidatable ivalidatable)
        {
            if (!ivalidatable.IsValid(out var reason))
                throw new ValidatableException(reason);
        }

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
            switch (entity)
            {
                case JObject j: return j;
                case DDictionary ddict: return ddict.ToJObject();
                case Dictionary<string, dynamic> _idict: return _idict.ToJObject();
                case IDictionary idict:
                    var _jobj = new JObject();
                    foreach (DictionaryEntry pair in idict)
                        _jobj[pair.Key.ToString()] = pair.Value == null
                            ? null
                            : JToken.FromObject(pair.Value, Serializer.JsonSerializer);
                    return _jobj;
            }
            var jobj = new JObject();
            entity.GetType()
                .GetStaticProperties()
                .Values
                .Where(p => !p.Hidden)
                .ForEach(prop =>
                {
                    object val = prop.GetValue(entity);
                    jobj[prop.Name] = val?.ToJToken();
                });
            return jobj;
        }

        internal static bool IsStarcounterCompatible(this Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Empty: return false;
                case TypeCode.DBNull: return false;
                case TypeCode.Object:
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        return IsStarcounterCompatible(type.GenericTypeArguments[0]);
                    return type.IsClass && type.HasAttribute<DatabaseAttribute>();
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
            }
            if (type == typeof(Binary)) return true;
            return false;
        }

        internal static string GetProviderId(this Type providerType)
        {
            var typeName = providerType.Name;
            if (typeName == null) throw new ArgumentNullException();
            if (typeName.EndsWith("provider", InvariantCultureIgnoreCase))
                typeName = typeName.Substring(0, typeName.Length - 8);
            return typeName;
        }

        internal static string GetProviderId(this ResourceProvider provider) => GetProviderId(provider.GetType());

        internal static Type GetWrappedType(this Type wrapperType) => wrapperType.BaseType?.GetGenericArguments()[0];

        internal static bool IsWrapper(this Type type) => typeof(IResourceWrapper).IsAssignableFrom(type);

        /// <summary>
        /// If the type is represented by some RESTar resource in the current instance,
        /// returns this resource. Else null.
        /// </summary>
        public static IResource GetResource(this Type type) => Resource.ByTypeName(type.FullName);

        /// <summary>
        /// If the type is represented by some RESTar resource in the current instance,
        /// returns the name of this resource. Else null.
        /// </summary>
        public static string GetResourceName(this Type type) => type.GetResource()?.Name;

        #endregion

        #region Filter and Process

        internal static IEnumerable<T> Filter<T>(this IEnumerable<T> entities, IFilter filter) where T : class
        {
            return filter?.Apply(entities) ?? entities;
        }

        internal static IEnumerable<JObject> Process<T>(this IEnumerable<T> entities, IProcessor[] processors)
            where T : class
        {
            return processors.Aggregate(default(IEnumerable<JObject>), (e, p) => e != null ? p.Apply(e) : p.Apply(entities));
        }

        internal static (string WhereString, object[] Values) MakeWhereClause<T>(this IEnumerable<Condition<T>> conds,
            out Dictionary<int, int> valuesAssignments)
            where T : class
        {
            var _valuesAssignments = new Dictionary<int, int>();
            var literals = new List<object>();
            var clause = string.Join(" AND ", conds.Where(c => !c.Skip).Select((c, index) =>
            {
                var (key, op, value) = (c.Term.DbKey.Fnuttify(), c.InternalOperator.SQL, (object) c.Value);
                if (value == null)
                {
                    switch (c.Operator)
                    {
                        case EQUALS:
                            op = "IS NULL";
                            break;
                        case NOT_EQUALS:
                            op = "IS NOT NULL";
                            break;
                        default: throw new Exception($"Operator '{op}' is not valid for comparison with NULL");
                    }
                    return $"t.{key} {op}";
                }

                literals.Add(c.Value);
                _valuesAssignments[index] = literals.Count - 1;
                return $"t.{key} {c.InternalOperator.SQL} ? ";
            }));
            if (clause.Length == 0)
            {
                valuesAssignments = null;
                return (null, null);
            }
            valuesAssignments = _valuesAssignments;
            return ($"WHERE {clause}", literals.ToArray());
        }

        internal static (string WhereString, object[] Values) MakeWhereClause<T>(this IEnumerable<Condition<T>> conds)
            where T : class
        {
            var literals = new List<object>();
            var clause = string.Join(" AND ", conds.Where(c => !c.Skip).Select(c =>
            {
                var (key, op, value) = (c.Term.DbKey.Fnuttify(), c.InternalOperator.SQL, (object) c.Value);
                if (value == null)
                {
                    switch (c.Operator)
                    {
                        case EQUALS:
                            op = "IS NULL";
                            break;
                        case NOT_EQUALS:
                            op = "IS NOT NULL";
                            break;
                        default: throw new Exception($"Operator '{op}' is not valid for comparison with NULL");
                    }
                    return $"t.{key} {op}";
                }
                literals.Add(c.Value);
                return $"t.{key} {c.InternalOperator.SQL} ? ";
            }));
            return clause.Length > 0 ? ($"WHERE {clause}", literals.ToArray()) : (null, null);
        }

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
        /// Gets the value of a key from an IDictionary, or null if the dictionary does not contain the key.
        /// </summary>
        private static dynamic SafeGet(this IDictionary dict, string key)
        {
            return dict.Contains(key) ? dict[key] : null;
        }

        /// <summary>
        /// Gets the value of a key from an IDictionary, without case sensitivity, or throws a KeyNotFoundException
        /// if the dictionary does not contain the key.
        /// </summary>
        public static T GetNoCase<T>(this IDictionary<string, T> dict, string key)
        {
            return dict.First(pair => pair.Key.EqualsNoCase(key)).Value;
        }


        /// <summary>
        /// Gets the value of a key from an IDictionary, without case sensitivity, or null if the dictionary does 
        /// not contain the key.
        /// </summary>
        public static T SafeGetNoCase<T>(this IDictionary<string, T> dict, string key)
        {
            var matches = dict.Where(pair => pair.Key.EqualsNoCase(key)).ToList();
            switch (matches.Count)
            {
                case 0: return default;
                case 1: return matches[0].Value;
                default: return dict.SafeGet(key);
            }
        }

        /// <summary>
        /// Gets the value of a key from an IDictionary, without case sensitivity, or null if the dictionary does 
        /// not contain the key.
        /// </summary>
        public static dynamic SafeGetNoCase(this IDictionary dict, string key, out string actualKey)
        {
            var matches = dict.Keys.Cast<string>().Where(k => k.EqualsNoCase(key)).ToList();
            if (matches.Count > 1)
            {
                var val = dict.SafeGet(key);
                if (val == null)
                {
                    actualKey = null;
                    return null;
                }
                actualKey = key;
                return val;
            }
            var match = matches.FirstOrDefault();
            actualKey = match;
            return match == null ? null : dict[match];
        }

        /// <summary>
        /// Gets the value of a key from an IDictionary, without case sensitivity, or null if the dictionary does 
        /// not contain the key. The actual key is returned in the actualKey out parameter.
        /// </summary>
        public static T SafeGetNoCase<T>(this IDictionary<string, T> dict, string key, out string actualKey)
        {
            var matches = dict.Where(pair => pair.Key.EqualsNoCase(key)).ToList();
            if (matches.Count > 1)
            {
                var val = dict.SafeGet(key);
                if (val == null)
                {
                    actualKey = null;
                    return default;
                }
                actualKey = key;
                return val;
            }
            var match = matches.FirstOrDefault();
            actualKey = match.Key;
            return match.Value;
        }


        /// <summary>
        /// Tries to get the value of a key from an IDictionary, without case sensitivity
        /// </summary>
        public static bool TryGetNoCase<T>(this IDictionary<string, T> dict, string key, out T result)
        {
            result = default;
            var matches = dict.Where(pair => pair.Key.EqualsNoCase(key)).ToList();
            switch (matches.Count)
            {
                case 0: return false;
                case 1:
                    result = matches[0].Value;
                    return true;
                default: return dict.TryGetValue(key, out result);
            }
        }


        /// <summary>
        /// Tries to get the value of a key from an IDictionary, without case sensitivity, and returns
        /// the actual key of the key value pair (if found).
        /// </summary>
        public static bool TryGetNoCase(this IDictionary dict, string key, out string actualKey, out dynamic result)
        {
            result = default(object);
            actualKey = null;
            var matches = dict.Keys.Cast<string>().Where(k => k.EqualsNoCase(key)).ToList();
            switch (matches.Count)
            {
                case 0: return false;
                case 1:
                    actualKey = matches[0];
                    result = dict[actualKey];
                    return true;
                default:
                    if (!dict.Contains(key))
                        return false;
                    actualKey = key;
                    result = dict[key];
                    return true;
            }
        }

        /// <summary>
        /// Gets the value of a key from an IDictionary, without case sensitivity, or null if the dictionary does 
        /// not contain the key. The actual key is returned in the actualKey out parameter.
        /// </summary>
        public static bool TryGetNoCase<T>(this IDictionary<string, T> dict, string key, out string actualKey,
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


        /// <summary>
        /// Converts a DDictionary object to a JSON.net JObject
        /// </summary>
        public static JObject ToJObject(this DDictionary d)
        {
            var jobj = new JObject();
            d.KeyValuePairs.ForEach(pair => jobj[pair.Key] = MakeJToken(pair.Value));
            return jobj;
        }

        /// <summary>
        /// Converts a Dictionary object to a JSON.net JObject
        /// </summary>
        public static JObject ToJObject(this Dictionary<string, dynamic> d)
        {
            var jobj = new JObject();
            d.ForEach(pair => jobj[pair.Key] = MakeJToken(pair.Value));
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

        internal static string MatchKey(this IDictionary dict, string key)
        {
            return dict.Keys.Cast<string>().FirstOrDefault(k => key == k);
        }

        private static IEnumerable<DictionaryEntry> Cast(this IDictionary dict) => dict.Cast<DictionaryEntry>();

        internal static string MatchKeyIgnoreCase_IDict(this IDictionary dict, string key)
        {
            var matches = dict.Cast().Where(pair => pair.Key.ToString().EqualsNoCase(key)).ToList();
            return matches.Count > 1 ? dict.MatchKey(key) : (string) matches.FirstOrDefault().Key;
        }

        internal static string MatchKey<T>(this IDictionary<string, T> dict, string key)
        {
            return dict.Keys.FirstOrDefault(k => key == k);
        }

        internal static string MatchKeyIgnoreCase<T>(this IDictionary<string, T> dict, string key)
        {
            var matches = dict.Where(pair => pair.Key.EqualsNoCase(key)).ToList();
            return matches.Count > 1 ? dict.MatchKey(key) : matches.FirstOrDefault().Key;
        }

        #endregion

        #region Requests

        private static readonly CultureInfo en_US = new CultureInfo("en-US");

        internal static dynamic ParseConditionValue(this string str)
        {
            switch (str)
            {
                case null: return null;
                case "null": return null;
                case "": throw new SyntaxException(InvalidConditionSyntax, "No condition value literal after operator");
                case var escaped when escaped[0] == '\"' && escaped[escaped.Length - 1] == '\"':
                    return escaped.Remove(0, 1).Remove(escaped.Length - 2, 1);
                case var _ when bool.TryParse(str, out var @bool): return @bool;
                case var _ when int.TryParse(str, out var @int): return @int;
                case var _ when decimal.TryParse(str, Float, en_US, out var dec): return dec;
                case var _ when DateTime.TryParseExact(str, "yyyy-MM-dd", null, AssumeUniversal, out var dat) ||
                                DateTime.TryParseExact(str, "yyyy-MM-ddTHH:mm:ss", null, AssumeUniversal, out dat) ||
                                DateTime.TryParseExact(str, "O", null, AssumeUniversal, out dat): return dat;
                default: return str;
            }
        }

        internal static void MethodCheck(this IRequest request)
        {
            if (!Authenticator.MethodCheck(request.Method, request.Resource, request.AuthToken))
                throw Authenticator.NotAuthorizedException;
        }

        /// <summary>
        /// Returns true if and only if the request contains a condition with the given key and 
        /// operator (case insensitive). If true, the out Condition parameter will contain a reference to the found
        /// condition.
        /// </summary>
        public static bool TryGetCondition<T>(this IRequest<T> request, string key, Operators op,
            out Condition<T> condition) where T : class
        {
            condition = request.Conditions?.Get(key, op);
            return condition != null;
        }

        /// <summary>
        /// Returns true if and only if the request contains at least one condition with the given key (case insensitive). 
        /// If true, the out Conditions parameter will contain all the matching conditions
        /// </summary>
        /// <returns></returns>
        public static bool TryGetConditions<T>(this IRequest<T> request, string key,
            out ICollection<Condition<T>> conditions) where T : class
        {
            conditions = request.Conditions.Get(key).ToList();
            return !conditions.Any() != true;
        }

        internal static (ErrorCodes Code, Response Response) GetError(this Exception ex)
        {
            switch (ex)
            {
                case RESTarException re: return (re.ErrorCode, re.Response);
                case FormatException _: return (UnsupportedContent, BadRequest(ex));
                case JsonReaderException _: return (FailedJsonDeserialization, JsonError);
                case DbException _: return (DatabaseError, DbError(ex));
                default: return (Unknown, InternalError(ex));
            }
        }

        #endregion

        #region Conversion

        internal static string SHA256(this string input)
        {
            using (var hasher = System.Security.Cryptography.SHA256.Create())
                return Convert.ToBase64String(hasher.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        internal static byte[] ToBytes(this string str) => Encoding.UTF8.GetBytes(str);

        internal static string TotalMessage(this Exception e)
        {
            var message = new StringBuilder(e.Message);
            var ie = e.InnerException;
            while (ie != null)
            {
                if (!string.IsNullOrWhiteSpace(ie.Message))
                    message.Append(ie.Message);
                if (ie.InnerException != null)
                    message.Append(" | ");
                ie = ie.InnerException;
            }
            return message.ToString().Replace("\r\n", " | ");
        }

        internal static byte[] ToByteArray(this Stream stream)
        {
            MemoryStream ms;
            if (stream is MemoryStream _ms) ms = _ms;
            else
            {
                ms = new MemoryStream();
                using (stream) stream.CopyTo(ms);
            }
            return ms.ToArray();
        }

        internal static ClosedXML.Excel.XLWorkbook ToExcel(this IEnumerable<object> entities, IResource resource)
        {
            var dataSet = new DataSet();
            var table = entities.MakeDataTable(resource);
            if (table.Rows.Count == 0) return null;
            dataSet.Tables.Add(table);
            var workbook = new ClosedXML.Excel.XLWorkbook();
            workbook.AddWorksheet(dataSet);
            return workbook;
        }

        /// <summary>
        /// Converts an IEnumerable of T to an Excel workbook
        /// </summary>
        public static ClosedXML.Excel.XLWorkbook ToExcel<T>(this IEnumerable<T> entities) where T : class
        {
            var resource = Resource<T>.Get;
            var dataSet = new DataSet();
            var table = entities.MakeDataTable(resource);
            if (table.Rows.Count == 0) return null;
            dataSet.Tables.Add(table);
            var workbook = new ClosedXML.Excel.XLWorkbook();
            workbook.AddWorksheet(dataSet);
            return workbook;
        }

        internal static DataTable MakeDataTable(this IEnumerable<object> entities, IResource resource)
        {
            var table = new DataTable();
            switch (entities)
            {
                case IEnumerable<IDictionary<string, object>> dicts:
                    foreach (var item in dicts)
                    {
                        var row = table.NewRow();
                        foreach (var pair in item)
                        {
                            if (!table.Columns.Contains(pair.Key))
                                table.Columns.Add(pair.Key);
                            row[pair.Key] = pair.Value.MakeDynamicCellValue();
                        }
                        table.Rows.Add(row);
                    }
                    return table;
                case IEnumerable<JObject> jobjects:
                    foreach (var item in jobjects)
                    {
                        var row = table.NewRow();
                        foreach (var pair in item)
                        {
                            if (!table.Columns.Contains(pair.Key))
                                table.Columns.Add(pair.Key);
                            row[pair.Key] = pair.Value.ToObject<object>().MakeDynamicCellValue();
                        }
                        table.Rows.Add(row);
                    }
                    return table;
                default:
                    var properties = resource.Type.GetStaticProperties().Values
                        .Where(p => !p.Hidden)
                        .ToList();
                    properties.ForEach(prop => table.Columns.Add(prop.MakeColumn()));
                    entities.ForEach(item =>
                    {
                        var row = table.NewRow();
                        properties.ForEach(prop => prop.WriteCell(row, item));
                        table.Rows.Add(row);
                    });
                    return table;
            }
        }

        internal static object MakeDynamicCellValue(this object value)
        {
            switch (value)
            {
                case bool _:
                case decimal _:
                case long _:
                case string _: return value;
                case sbyte other: return (long) other;
                case byte other: return (long) other;
                case short other: return (long) other;
                case ushort other: return (long) other;
                case int other: return (long) other;
                case uint other: return (long) other;
                case ulong other: return (long) other;
                case float other: return (decimal) other;
                case double other: return (decimal) other;
                case char other: return other.ToString();
                case DateTime other: return other.ToString("O");
                case JObject _: return typeof(JObject).FullName;
                case DDictionary _: return $"$(ObjectID: {value.GetObjectID()})";
                case IDictionary other: return other.GetType().FullName;
                case IEnumerable<object> other: return string.Join(", ", other.Select(o => o.ToString()));
                case DBNull _:
                case null: return DBNull.Value;
                case var enumArr when value.GetType().Implements(typeof(IEnumerable<>), out var p) && p.Any() && p[0].IsEnum:
                    IEnumerable<object> objects = System.Linq.Enumerable.Cast<object>((dynamic) enumArr);
                    return string.Join(", ", objects.Select(o => o.ToString()));
                default: return Do.Try(() => $"$(ObjectID: {value.GetObjectID()})", value.ToString);
            }
        }

        /// <summary>
        /// Converts an HTTP status code to the underlying numeric code
        /// </summary>
        internal static ushort? ToCode(this HttpStatusCode statusCode) => (ushort) statusCode;

        #endregion

        #region View models

        internal static Json MakeCurrentView(this RESTarView view)
        {
            var master = Self.GET<View.Page>("/__restar/__page");
            master.CurrentPage = view ?? master.CurrentPage;
            return master;
        }

        internal static Dictionary<string, dynamic> MakeViewModelTemplate(this IResource resource)
        {
            if (resource.IsDDictionary) return new Dictionary<string, dynamic>();
            return resource.Type.GetStaticProperties().Values
                .Where(p => !p.Hidden || p is SpecialProperty)
                .ToDictionary(p => p.ViewModelName, p => p.Type.MakeViewModelDefault(p));
        }

        internal static dynamic MakeViewModelDefault(this Type type, StaticProperty property = null)
        {
            dynamic DefaultValueRecurser(Type propType)
            {
                if (propType == typeof(string))
                    return "";
                var ienumImplementation = propType.GetInterfaces()
                    .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                if (ienumImplementation != null)
                {
                    var elementType = ienumImplementation.GenericTypeArguments[0];
                    return new object[] {DefaultValueRecurser(elementType)};
                }
                if (propType.IsClass)
                {
                    if (propType == typeof(object))
                        return "@RESTar()";
                    var props = propType.GetStaticProperties().Values;
                    return props.ToDictionary(
                        p => p.ViewModelName,
                        p => DefaultValueRecurser(p.Type));
                }
                if (propType.IsValueType)
                {
                    if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        return propType.GetGenericArguments()[0].GetDefault();
                    return propType.GetDefault();
                }
                throw new ArgumentOutOfRangeException();
            }

            return DefaultValueRecurser(type);
        }

        #endregion
    }
}