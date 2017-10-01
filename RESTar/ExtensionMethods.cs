using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Dynamit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Admin;
using RESTar.Auth;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Serialization;
using RESTar.View;
using Starcounter;
using static System.Globalization.DateTimeStyles;
using static System.Globalization.NumberStyles;
using static System.Reflection.BindingFlags;
using static System.StringComparison;
using static RESTar.Internal.ErrorCodes;
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
        #region Operation finders

        private static Exception InvalidImplementation(string i, string r, Type f) => new Exception(
            $"Invalid {i} implementation for resource '{r}'. Expected '{i}<{r}>', but found '{i}<{f.FullName}>'");

        internal static Selector<T> GetSelector<T>(this Type type) where T : class
        {
            if (!type.Implements(typeof(ISelector<>), out var p)) return null;
            if (p[0] != typeof(T)) throw InvalidImplementation("ISelector", type.FullName, p[0]);
            return (Selector<T>) type.GetMethod("Select", Instance | Public).CreateDelegate(typeof(Selector<T>), null);
        }

        internal static Inserter<T> GetInserter<T>(this Type type) where T : class
        {
            if (!type.Implements(typeof(IInserter<>), out var p)) return null;
            if (p[0] != typeof(T)) throw InvalidImplementation("IInserter", type.FullName, p[0]);
            return (Inserter<T>) type.GetMethod("Insert", Instance | Public).CreateDelegate(typeof(Inserter<T>), null);
        }

        internal static Updater<T> GetUpdater<T>(this Type type) where T : class
        {
            if (!type.Implements(typeof(IUpdater<>), out var p)) return null;
            if (p[0] != typeof(T)) throw InvalidImplementation("IUpdater", type.FullName, p[0]);
            return (Updater<T>) type.GetMethod("Update", Instance | Public).CreateDelegate(typeof(Updater<T>), null);
        }

        internal static Deleter<T> GetDeleter<T>(this Type type) where T : class
        {
            if (!type.Implements(typeof(IDeleter<>), out var p)) return null;
            if (p[0] != typeof(T)) throw InvalidImplementation("IDeleter", type.FullName, p[0]);
            return (Deleter<T>) type.GetMethod("Delete", Instance | Public).CreateDelegate(typeof(Deleter<T>), null);
        }

        internal static Counter<T> GetCounter<T>(this Type type) where T : class
        {
            if (!type.Implements(typeof(ICounter<>), out var p)) return null;
            if (p[0] != typeof(T)) throw InvalidImplementation("ICounter", type.FullName, p[0]);
            return (Counter<T>) type.GetMethod("Count", Instance | Public).CreateDelegate(typeof(Counter<T>), null);
        }

        #endregion

        #region Reflection

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

        internal static bool HasAttribute<TAttribute>(this MemberInfo type)
            where TAttribute : Attribute => (type?.GetCustomAttributes<TAttribute>().Any()).GetValueOrDefault();

        internal static bool HasAttribute<TAttribute>(this MemberInfo type, out TAttribute attribute)
            where TAttribute : Attribute
        {
            attribute = type?.GetCustomAttributes<TAttribute>().FirstOrDefault();
            return attribute != null;
        }

        internal static bool Implements(this Type type, Type interfaceType)
        {
            return type.GetInterfaces()
                .Any(i => i.Name == interfaceType.Name && i.Namespace == interfaceType.Namespace);
        }

        internal static bool Implements(this Type type, Type interfaceType, out Type[] genericParameters)
        {
            var @interface = type.GetInterfaces()
                .FirstOrDefault(i => i.Name == interfaceType.Name && i.Namespace == interfaceType.Namespace);
            genericParameters = @interface?.GetGenericArguments();
            return @interface != null;
        }

        #endregion

        #region Other

        internal static string UriDecode(this string str) => HttpUtility.UrlDecode(str);
        internal static string UriEncode(this string str) => HttpUtility.UrlEncode(str);

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

        internal static string RemoveTabsAndBreaks(this string input) => input != null
            ? Regex.Replace(input, @"\t|\n|\r", "")
            : null;

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

        #endregion

        #region Resource helpers

        internal static bool IsDDictionary(this Type type) => type == typeof(DDictionary) ||
                                                              type.IsSubclassOf(typeof(DDictionary));

        internal static bool IsStarcounter(this Type type) => type.HasAttribute<DatabaseAttribute>();

        internal static string RESTarMemberName(this MemberInfo m) => m.GetAttribute<DataMemberAttribute>()?.Name ??
                                                                      m.Name;

        internal static void Validate(this IValidatable ivalidatable)
        {
            if (!ivalidatable.IsValid(out var reason))
                throw new ValidatableException(reason);
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
                .Where(p => !(p is SpecialProperty))
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

        #endregion

        #region Filter and Process

        /// <summary>
        /// Applies this list of conditions to an IEnumerable of entities and returns
        /// the entities for which all the conditions hold.
        /// </summary>
        internal static IEnumerable<T> Apply<T>(this IEnumerable<Condition<T>> conditions, IEnumerable<T> entities)
            where T : class
        {
            return entities.Where(entity => conditions.All(condition => condition.HoldsFor(entity)));
        }

        internal static IEnumerable<T> Filter<T>(this IEnumerable<T> entities, IFilter filter) where T : class
        {
            return filter?.Apply(entities) ?? entities;
        }

        internal static IEnumerable<JObject> Process<T>(this IEnumerable<T> entities, IProcessor[] processors)
            where T : class => processors
            .Aggregate(default(IEnumerable<JObject>), (e, p) => e != null ? p.Apply(e) : p.Apply(entities));

        internal static (string WhereString, object[] Values) MakeWhereClause<T>(this IEnumerable<Condition<T>> conds,
            out Dictionary<int, int> valuesAssignments)
            where T : class
        {
            var _valuesAssignments = new Dictionary<int, int>();
            var Values = new List<object>();
            var WhereString = string.Join(" AND ", conds.Where(c => !c.Skip).Select((c, index) =>
            {
                var key = c.Term.DbKey.Fnuttify();
                if (c.Value == null)
                    return $"t.{key} {(c.Operator == Operator.NOT_EQUALS ? "IS NOT NULL" : "IS NULL")}";
                Values.Add(c.Value);
                _valuesAssignments[index] = Values.Count - 1;
                return $"t.{key} {c.Operator.SQL}?";
            }));
            if (WhereString == "")
            {
                valuesAssignments = null;
                return (null, null);
            }
            valuesAssignments = _valuesAssignments;
            return ($"WHERE {WhereString}", Values.ToArray());
        }

        internal static (string WhereString, object[] Values) MakeWhereClause<T>(this IEnumerable<Condition<T>> conds)
            where T : class
        {
            var Values = new List<object>();
            var WhereString = string.Join(" AND ", conds.Where(c => !c.Skip).Select(c =>
            {
                var key = c.Term.DbKey.Fnuttify();
                if (c.Value == null)
                    return $"t.{key} {(c.Operator == Operator.NOT_EQUALS ? "IS NOT NULL" : "IS NULL")}";
                Values.Add(c.Value);
                return $"t.{key} {c.Operator.SQL}?";
            }));
            if (WhereString == "") return (null, null);
            return ($"WHERE {WhereString}", Values.ToArray());
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
        public static dynamic SafeGet(this IDictionary dict, string key)
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
            d.KeyValuePairs.ForEach(pair => jobj[pair.Key] = (JToken) pair.Value);
            return jobj;
        }

        /// <summary>
        /// Converts a Dictionary object to a JSON.net JObject
        /// </summary>
        public static JObject ToJObject(this Dictionary<string, dynamic> d)
        {
            var jobj = new JObject();
            d.ForEach(pair => jobj[pair.Key] = (JToken) pair.Value);
            return jobj;
        }

        internal static string MatchKey(this IDictionary dict, string key)
        {
            return dict.Keys.Cast<string>().FirstOrDefault(k => key == k);
        }

        private static IEnumerable<DictionaryEntry> Cast(this IDictionary dict)
        {
            foreach (DictionaryEntry item in dict) yield return item;
        }

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

        internal static bool IsInternal<T>(this IRequest<T> request) where T : class => request is Request<T>;

        internal static bool IsExternal<T>(this IRequest<T> request) where T : class => !request.IsInternal();

        private static readonly CultureInfo en_US = new CultureInfo("en-US");

        internal static dynamic ParseConditionValue(this string str)
        {
            switch (str)
            {
                case null: return null;
                case "null": return null;
                case "": throw new SyntaxException(InvalidConditionSyntax, "No condition value literal after operator");
                case var s when s[0] == '\"' && s[s.Length - 1] == '\"': return s.Remove(0, 1).Remove(s.Length - 2, 1);
                case var _ when bool.TryParse(str, out var @bool): return @bool;
                case var _ when int.TryParse(str, out var @int): return @int;
                case var _ when decimal.TryParse(str, Float, en_US, out var dec): return dec;
                case var _ when DateTime.TryParseExact(str, "yyyy-MM-dd", null, AssumeUniversal, out var dat) ||
                                DateTime.TryParseExact(str, "yyyy-MM-ddTHH:mm:ss", null, AssumeUniversal, out dat) ||
                                DateTime.TryParseExact(str, "O", null, AssumeUniversal, out dat): return dat;
                default: return str;
            }
        }

        internal static Args ToArgs(this string query, Request request) => new Args(query, request);

        private static string CheckQuery(this string query, Request request)
        {
            if (query.Count(c => c == '/') > 3)
                throw new SyntaxException(InvalidSeparator,
                    "Invalid argument separator count. A RESTar URI can contain at most 3 " +
                    $"forward slashes after the base uri. URI scheme: {Settings._ResourcesPath}" +
                    "/[resource]/[conditions]/[meta-conditions]");
            if (request.HeadersDictionary.ContainsKey("X-ARR-LOG-ID"))
                return query.Replace("%25", "%");
            return query;
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
        public static bool TryGetCondition<T>(this IRequest<T> request, string key, Operator op,
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

        /// <summary>
        /// If the resource is a static Starcounter resource, returns an SQL query for the request.
        /// </summary>
        public static (string SQL, object[] Values) GetSQL<T>(this IRequest<T> request) where T : class
        {
            if (request.Resource.ResourceType != RESTarResourceType.StaticStarcounter)
                throw new ArgumentException("Can only get SQL for static Starcounter resources. Resource " +
                                            $"'{request.Resource.Name}' is of type {request.Resource.ResourceType}");
            var whereClause = request.Conditions.MakeWhereClause();
            return ($"SELECT t FROM {typeof(T).FullName} t " +
                    $"{whereClause.WhereString} " +
                    $"{request.MetaConditions.OrderBy?.SQL} " +
                    $"{request.MetaConditions.Limit.SQL}", whereClause.Values);
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

        internal static byte[] ToBytes(this string json) => Encoding.UTF8.GetBytes(json);

        internal static string TotalMessage(this Exception e)
        {
            var message = new StringBuilder(e.Message);
            var ie = e.InnerException;
            while (ie != null)
            {
                if (!string.IsNullOrWhiteSpace(ie.Message))
                    message.Append(ie.Message);
                if (ie.InnerException != null)
                    message.Append(" : ");
                ie = ie.InnerException;
            }
            return message.ToString();
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

        /// <summary>
        /// Serializes an Excel workbook to a byte array
        /// </summary>
        public static byte[] SerializeExcel(this ClosedXML.Excel.XLWorkbook excel)
        {
            using (var memstream = new MemoryStream())
            {
                excel.SaveAs(memstream);
                return memstream.ToArray();
            }
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
                    var properties = resource.GetStaticProperties().Values;
                    foreach (var prop in properties)
                        table.Columns.Add(prop.MakeColumn());
                    foreach (var item in entities)
                    {
                        var row = table.NewRow();
                        properties.ForEach(prop => prop.WriteCell(row, item));
                        table.Rows.Add(row);
                    }
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
                default: return Do.Try(() => $"$(ObjectID: {value.GetObjectID()})", value.ToString);
            }
        }

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
            if (resource.IsDDictionary)
                return new Dictionary<string, dynamic>();
            var properties = resource.GetStaticProperties().Values;
            return properties.ToDictionary(
                p => p.ViewModelName,
                p => p.Type.MakeViewModelDefault(p)
            );
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