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
using ClosedXML.Excel;
using Dynamit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Auth;
using RESTar.Deflection;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.View;
using Starcounter;
using static System.Reflection.BindingFlags;
using static System.StringComparison;
using static RESTar.RESTarMethods;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Requests.Responses;
using static RESTar.RESTarConfig;
using static Starcounter.DbHelper;
using static RESTar.Deflection.TypeCache;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    /// <summary>
    /// Extension methods used by RESTar
    /// </summary>
    public static class ExtensionMethods
    {
        static ExtensionMethods()
        {
            DEFAULT_MAKER = typeof(ExtensionMethods).GetMethod(nameof(DEFAULT), NonPublic | Static);
        }

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

        internal static bool Implements(this Type type, Type interfaceType, out Type[] genericParameters)
        {
            var @interface = type.GetInterface(interfaceType.FullName);
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
        internal static string ToMethodsString(this IEnumerable<RESTarMethods> ie) => string.Join(", ", ie);

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

        internal static RESTarMethods[] ToMethodsArray(this string methodsString)
        {
            if (methodsString == null) return null;
            if (methodsString.Trim() == "*")
                return Methods;
            return methodsString.Split(',').Select(s => (RESTarMethods) Enum.Parse(typeof(RESTarMethods), s)).ToArray();
        }

        internal static RESTarMethods[] ToMethods(this RESTarPresets preset)
        {
            switch (preset)
            {
                case RESTarPresets.ReadOnly: return new[] {GET};
                case RESTarPresets.WriteOnly: return new[] {POST, DELETE};
                case RESTarPresets.ReadAndUpdate: return new[] {GET, PATCH};
                case RESTarPresets.ReadAndWrite: return Methods;
                default: throw new ArgumentOutOfRangeException(nameof(preset));
            }
        }

        internal static object GetDefault(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return DEFAULT_MAKER.MakeGenericMethod(type).Invoke(null, null);
        }

        private static readonly MethodInfo DEFAULT_MAKER;

        private static object DEFAULT<T>() => default(T);

        internal static AccessRights ToAccessRights(this IEnumerable<AccessRight> accessRights)
        {
            var _accessRights = new AccessRights();
            foreach (var right in accessRights)
            {
                foreach (var resource in right.Resources)
                {
                    _accessRights[resource] = _accessRights.ContainsKey(resource)
                        ? _accessRights[resource].Union(right.AllowedMethods).ToArray()
                        : right.AllowedMethods;
                }
            }
            return _accessRights;
        }

        #endregion

        #region Resource helpers

        internal static Term MakeTerm(this IResource resource, string key, bool dynamicUnknowns)
        {
            return resource.Type.MakeTerm(key, dynamicUnknowns);
        }

        internal static Term MakeTerm(this Type resource, string key, bool dynamicUnknowns)
        {
            var hash = resource.GetHashCode() + key.ToLower().GetHashCode() +
                       dynamicUnknowns.GetHashCode();
            if (!TermCache.TryGetValue(hash, out Term term))
                term = TermCache[hash] = Term.ParseInternal(resource, key, dynamicUnknowns);
            return term;
        }

        internal static bool IsDDictionary(this Type type) => type == typeof(DDictionary) ||
                                                              type.IsSubclassOf(typeof(DDictionary));

        internal static bool IsStarcounter(this Type type) => type.HasAttribute<DatabaseAttribute>();

        internal static string RESTarMemberName(this MemberInfo m) => m.GetAttribute<DataMemberAttribute>()?.Name ??
                                                                      m.Name;

        internal static void RunValidation(this IValidatable ivalidatable)
        {
            if (!ivalidatable.Validate(out string reason))
                throw new ValidatableException(reason);
        }

        internal static ICollection<IResource> FindResources(this string searchString)
        {
            searchString = searchString.ToLower();
            var asterisks = searchString.Count(i => i == '*');
            if (asterisks > 1)
                throw new Exception("Invalid resource string syntax");
            if (asterisks == 1)
            {
                if (searchString.Last() != '*')
                    throw new Exception("Invalid resource string syntax");
                var commonPart = searchString.Split('*')[0];
                var matches = ResourceByName
                    .Where(pair => pair.Key.StartsWith(commonPart))
                    .Select(pair => pair.Value)
                    .Union(DB.All<ResourceAlias>()
                        .Where(alias => alias.Alias.StartsWith(commonPart))
                        .Select(alias => alias.IResource))
                    .ToList();
                if (matches.Any()) return matches;
                throw new UnknownResourceException(searchString);
            }
            var resource = ResourceAlias.ByAlias(searchString)?.IResource;
            if (resource == null)
                ResourceByName.TryGetValue(searchString, out resource);
            if (resource != null)
                return new[] {resource};
            throw new UnknownResourceException(searchString);
        }

        internal static IResource FindResource(this string searchString)
        {
            searchString = searchString.ToLower();
            var resource = ResourceAlias.ByAlias(searchString)?.IResource;
            if (resource == null)
                ResourceByName.TryGetValue(searchString, out resource);
            if (resource != null)
                return resource;
            var keys = ResourceByName.Keys
                .Where(key => key.EndsWith($".{searchString}"))
                .ToList();
            if (keys.Count < 1)
                throw new UnknownResourceException(searchString);
            if (keys.Count > 1)
                throw new AmbiguousResourceException(searchString,
                    keys.Select(k => ResourceByName[k].Name).ToList());
            return ResourceByName[keys[0]];
        }

        internal static JObject ToJObject(this IEnumerable<JProperty> props) => new JObject(props);

        /// <summary>
        /// Converts an IEnumerable of resource entities to JSON.net JObjects.
        /// </summary>
        public static IEnumerable<JObject> ToJObjects(this IEnumerable<object> entities) =>
            entities.Select(ToJObject);

        /// <summary>
        /// Converts a resource entitiy to a JSON.net JObject.
        /// </summary>
        public static JObject ToJObject(this object entity)
        {
            if (entity is JObject j) return j;
            if (entity is DDictionary ddict) return ddict.ToJObject();
            if (entity is Dictionary<string, dynamic> _idict) return _idict.ToJObject();
            JObject jobj;
            if (entity is IDictionary idict)
            {
                jobj = new JObject();
                foreach (DictionaryEntry pair in idict)
                    jobj[pair.Key.ToString()] = pair.Value == null
                        ? null
                        : JToken.FromObject(pair.Value, Serializer.JsonSerializer);
                return jobj;
            }
            jobj = new JObject();
            entity.GetType()
                .GetStaticProperties()
                .Values
                .Where(p => !(p is SpecialProperty))
                .ForEach(prop =>
                {
                    var val = prop.Get(entity);
                    jobj[prop.Name] = val == null ? null : JToken.FromObject(val, Serializer.JsonSerializer);
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
        /// Filters an IEnumerable of resource entities and returns all entities x such that all the 
        /// conditions are true of x.
        /// </summary>
        public static IEnumerable<T> Where<T>(this IEnumerable<T> entities, Conditions<T> conditions) where T : class
        {
            if (conditions?.Any != true) return entities;
            return conditions.Apply(entities);
        }

        internal static IEnumerable<T> Filter<T>(this IEnumerable<T> entities, IFilter filter)
        {
            return filter?.Apply((dynamic) entities) ?? entities;
        }

        internal static IEnumerable<dynamic> Process<T>(this IEnumerable<T> entities, IProcessor processor)
        {
            return processor?.Apply((dynamic) entities) ?? (IEnumerable<dynamic>) entities;
        }

        internal static (string WhereString, object[] Values) MakeWhereClause<T>(this IEnumerable<Condition<T>> conds)
            where T : class
        {
            if (!conds.Any()) return (null, null);
            var Values = new List<object>();
            var WhereString = string.Join(" AND ", conds.Select(c =>
            {
                var key = c.Term.DbKey.Fnuttify();
                if (c.Value == null)
                    return $"t.{key} {(c.Operator == Operator.NOT_EQUALS ? "IS NOT NULL" : "IS NULL")}";
                Values.Add(c.Value);
                return $"t.{key} {c.Operator.SQL}?";
            }));
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
            var matches = dict.Where(pair => pair.Key.EqualsNoCase(key));
            return matches.MoreThanOne() ? dict.SafeGet(key) : matches.FirstOrDefault().Value;
        }

        /// <summary>
        /// Gets the value of a key from an IDictionary, without case sensitivity, or null if the dictionary does 
        /// not contain the key.
        /// </summary>
        public static dynamic SafeGetNoCase(this IDictionary dict, string key, out string actualKey)
        {
            var matches = Do.TryAndThrow(() => dict.Keys.Cast<string>().Where(k => k.EqualsNoCase(key)),
                "Invalid key type in Dictionary resource. Must be string");
            if (matches.MoreThanOne())
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
            var matches = dict.Where(pair => pair.Key.EqualsNoCase(key));
            if (matches.MoreThanOne())
            {
                var val = dict.SafeGet(key);
                if (val == null)
                {
                    actualKey = null;
                    return default(T);
                }
                actualKey = key;
                return val;
            }
            var match = matches.FirstOrDefault();
            actualKey = match.Key;
            return match.Value;
        }

        /// <summary>
        /// Converts a DDictionary object to a JSON.net JObject
        /// </summary>
        public static JObject ToJObject(this DDictionary d)
        {
            var jobj = new JObject();
            d.KeyValuePairs.ForEach(pair => jobj[pair.Key] = pair.Value);
            return jobj;
        }

        /// <summary>
        /// Converts a Dictionary object to a JSON.net JObject
        /// </summary>
        public static JObject ToJObject(this Dictionary<string, dynamic> d)
        {
            var jobj = new JObject();
            d.ForEach(pair => jobj[pair.Key] = pair.Value);
            return jobj;
        }

        internal static string MatchKey<T>(this IDictionary<string, T> dict, string key)
        {
            return dict.Keys.FirstOrDefault(k => key == k);
        }

        internal static string MatchKeyIgnoreCase<T>(this IDictionary<string, T> dict, string key)
        {
            string _actualKey = null;
            var results = dict.Keys.Where(k =>
            {
                var equals = k.EqualsNoCase(key);
                if (equals) _actualKey = k;
                return equals;
            });
            var count = results.Count();
            switch (count)
            {
                case 0: return null;
                case 1: return _actualKey;
                default: return MatchKey(dict, key);
            }
        }

        #endregion

        #region IEnumerable

        internal static bool IsNullOrEmpty<T>(this IEnumerable<T> ienum) => ienum?.Any() != true;

        internal static string StringJoin<T>(this IEnumerable<T> source, string separator,
            Func<IEnumerable<T>, IEnumerable<string>> converter)
        {
            return string.Join(separator, converter(source));
        }

        internal static bool MoreThanOne<T>(this IEnumerable<T> source) => source?.Skip(1).Any() == true;

        internal static bool ExaclyOne<T>(this IEnumerable<T> source) => source?.Any() == true && !source.Skip(1).Any();

        internal static bool ContainsDuplicates<T>(this IEnumerable<T> source, out T duplicate)
        {
            duplicate = default(T);
            var d = new HashSet<T>();
            foreach (var t in source)
            {
                if (!d.Add(t))
                {
                    duplicate = t;
                    return true;
                }
            }
            return false;
        }

        internal static IEnumerable<T> Apply<T>(this IEnumerable<T> source, Action<T> action)
        {
            return source.Select(e =>
            {
                action(e);
                return e;
            });
        }

        internal static T2 Collect<T1, T2>(this IEnumerable<T1> source, Func<IEnumerable<T1>, T2> action)
        {
            return action(source);
        }

        internal static TResult CollectDict<TKey, TValue, TResult>(this IDictionary<TKey, TValue> source,
            Func<IDictionary<TKey, TValue>, TResult> action)
        {
            return action(source);
        }

        internal static IEnumerable<T> If<T>(this IEnumerable<T> source, Func<bool> predicate,
            Func<IEnumerable<T>, IEnumerable<T>> action)
        {
            return predicate() ? action(source) : source;
        }

        internal static IEnumerable<T> If<T>(this IEnumerable<T> source, Predicate<IEnumerable<T>> predicate,
            Func<IEnumerable<T>, IEnumerable<T>> action)
        {
            return predicate(source) ? action(source) : source;
        }

        internal static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var e in source) action(e);
        }

        internal static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            var i = 0;
            foreach (var e in source) action(e, i++);
        }

        #endregion

        #region Requests

        internal static dynamic GetConditionValue(this string valueString)
        {
            if (valueString == null) return null;
            if (valueString == "null") return null;
            if (valueString[0] == '\"' && valueString.Last() == '\"')
                return valueString.Remove(0, 1).Remove(valueString.Length - 2, 1);
            dynamic obj;
            if (bool.TryParse(valueString, out bool boo))
                obj = boo;
            else if (int.TryParse(valueString, out int _int))
                obj = _int;
            else if (decimal.TryParse(valueString, out decimal dec))
                obj = decimal.Round(dec, 6);
            else if (DateTime.TryParseExact(valueString, "yyyy-MM-dd", null, DateTimeStyles.AssumeUniversal,
                         out DateTime dat) ||
                     DateTime.TryParseExact(valueString, "yyyy-MM-ddTHH:mm:ss", null, DateTimeStyles.AssumeUniversal,
                         out dat) ||
                     DateTime.TryParseExact(valueString, "O", null, DateTimeStyles.AssumeUniversal, out dat))
                obj = dat;
            else obj = valueString;
            return obj;
        }


        internal static Args ToArgs(this string query, Request request) => new Args(query, request);

        private static string CheckQuery(this string query, Request request)
        {
            if (query.CharCount('/') > 3)
                throw new SyntaxException(InvalidSeparatorCount,
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

        internal static Conditions<T> ToConditions<T>(this IEnumerable<Condition<T>> conditions)
            where T : class
        {
            if (conditions?.Any() != true) return null;
            var _conditions = new Conditions<T>();
            _conditions.AddRange(conditions);
            return _conditions;
        }

        internal static Select ToSelect(this IEnumerable<Term> props)
        {
            if (props?.Any() != true) return null;
            var _props = new Select();
            var propsGroups = props.GroupBy(p => p.Key);
            _props.AddRange(propsGroups.Select(g => g.First()));
            return _props;
        }

        internal static Add ToAdd(this IEnumerable<Term> props)
        {
            if (props?.Any() != true) return null;
            var _props = new Add();
            var propsGroups = props.GroupBy(p => p.Key);
            _props.AddRange(propsGroups.Select(g => g.First()));
            return _props;
        }

        /// <summary>
        /// Returns true if and only if the request contains a condition with the given key and 
        /// operator (case insensitive). If true, the out Condition parameter will contain a reference to the found
        /// condition.
        /// </summary>
        public static bool TryGetCondition<T>(this IRequest<T> request, string key, Operator op,
            out Condition<T> condition) where T : class
        {
            condition = request.Conditions?[key, op];
            return condition != null;
        }

        /// <summary>
        /// Returns true if and only if the request contains at least one condition with the given key (case insensitive). 
        /// If true, the out Conditions parameter will contain all the matching conditions
        /// </summary>
        /// <returns></returns>
        public static bool TryGetConditions<T>(this IRequest<T> request, string key,
            out IEnumerable<Condition<T>> conditions) where T : class
        {
            conditions = request.Conditions?[key];
            return !conditions.IsNullOrEmpty();
        }

        /// <summary>
        /// If the resource is a static Starcounter resource, returns an SQL query for the request.
        /// </summary>
        public static void GetSQL<T>(this IRequest<T> request, out string SQL, out object[] Values) where T : class
        {
            if (request.Resource.ResourceType != RESTarResourceType.StaticStarcounter)
                throw new ArgumentException("Can only get SQL for static Starcounter resources. Resource " +
                                            $"'{request.Resource.Name}' was of type {request.Resource.ResourceType}");
            var whereClause = request.Conditions.MakeWhereClause();
            SQL = $"SELECT t FROM {typeof(T).FullName} t " +
                  $"{whereClause.WhereString} " +
                  $"{request.MetaConditions.OrderBy?.SQL} " +
                  $"{request.MetaConditions.Limit.SQL}";
            Values = whereClause.Values;
        }

        internal static (ErrorCodes Code, Response Response) GetError(this Exception ex)
        {
            switch (ex)
            {
                case RESTarException re: return (re.ErrorCode, re.Response);
                case FormatException _: return (UnsupportedContentType, BadRequest(ex));
                case JsonReaderException _: return (JsonDeserializationError, JsonError);
                case DbException _: return (DatabaseError, DbError(ex));
                default: return (UnknownError, InternalError(ex));
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

        internal static XLWorkbook ToExcel(this IEnumerable<object> entities, IResource resource)
        {
            var dataSet = new DataSet();
            dataSet.Tables.Add(entities.MakeTable(resource));
            var workbook = new XLWorkbook();
            workbook.AddWorksheet(dataSet);
            return workbook;
        }

        /// <summary>
        /// Converts an IEnumerable of T to an Excel workbook
        /// </summary>
        public static XLWorkbook ToExcel<T>(this IEnumerable<T> entities) where T : class
        {
            if (!ResourceByType.TryGetValue(typeof(T), out var resource))
                throw new UnknownResourceException(typeof(T).FullName);
            var dataSet = new DataSet();
            dataSet.Tables.Add(entities.MakeTable(resource));
            var workbook = new XLWorkbook();
            workbook.AddWorksheet(dataSet);
            return workbook;
        }

        /// <summary>
        /// Serializes an Excel workbook to a byte array
        /// </summary>
        public static byte[] SerializeExcel(this XLWorkbook excel)
        {
            using (var memstream = new MemoryStream())
            {
                excel.SaveAs(memstream);
                return memstream.ToArray();
            }
        }

        internal static DataTable MakeTable(this IEnumerable<object> entities, IResource resource)
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
                            row.SetCellValue(pair.Key, pair.Value);
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
                            row.SetCellValue(pair.Key, pair.Value.ToObject<object>());
                        }
                        table.Rows.Add(row);
                    }
                    return table;
                default:
                    var properties = resource.GetStaticProperties().Values;
                    foreach (var prop in properties)
                    {
                        var ColType = prop.Type.IsEnum || prop.Type.IsClass && prop.Type != typeof(string) ||
                                      prop.HasAttribute<ExcelFlattenToString>()
                            ? typeof(string)
                            : Nullable.GetUnderlyingType(prop.Type) ?? prop.Type;
                        table.Columns.Add(prop.Name, ColType);
                    }
                    foreach (var item in entities)
                    {
                        var row = table.NewRow();
                        foreach (var prop in properties)
                        {
                            var key = prop.Name;
                            object value;
                            if (prop.Type.IsEnum || prop.HasAttribute<ExcelFlattenToString>())
                                value = prop.Get(item)?.ToString();
                            else value = prop.Get(item);
                            row.SetCellValue(key, value);
                        }
                        table.Rows.Add(row);
                    }
                    return table;
            }
        }

        private static void SetCellValue(this DataRow row, string name, dynamic value)
        {
            if (value == null)
            {
                row[name] = DBNull.Value;
                return;
            }
            Type type = value.GetType();
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    var enumerable = value as IEnumerable<object>;
                    if (enumerable != null)
                        value = string.Join(", ", enumerable.Select(o => o.ToString()));
                    else
                    {
                        var array = value as Array;
                        if (array != null)
                            value = string.Join(", ", array.Cast<object>().Select(o => o.ToString()));
                    }
                    try
                    {
                        row[name] = value;
                    }
                    catch
                    {
                        try
                        {
                            row[name] = "$(ObjectID: " + DbHelper.GetObjectID(value) + ")";
                        }
                        catch
                        {
                            row[name] = value.ToString();
                        }
                    }
                    return;
                case TypeCode.DBNull:
                    row[name] = "";
                    return;
                case TypeCode.Boolean:
                case TypeCode.Decimal:
                case TypeCode.Int64:
                case TypeCode.String:
                    row[name] = value;
                    return;
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    row[name] = (long) value;
                    return;
                case TypeCode.Single:
                case TypeCode.Double:
                    row[name] = (decimal) value;
                    return;
                case TypeCode.DateTime:
                    var dateTime = (DateTime) value;
                    row[name] = dateTime.ToString("O");
                    return;
                case TypeCode.Char:
                    row[name] = value.ToString();
                    return;
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