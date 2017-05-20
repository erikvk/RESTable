using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Dynamit;
using Jil;
using RESTar.Auth;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;
using Starcounter;
using static System.Reflection.BindingFlags;
using static System.StringComparison;
using static Jil.DateTimeFormat;
using static Jil.UnspecifiedDateTimeKindBehavior;
using static RESTar.ResourceAlias;
using static RESTar.RESTarConfig;
using static RESTar.RESTarMethods;
using static RESTar.Settings;
using static Starcounter.DbHelper;
using Conditions = RESTar.Requests.Conditions;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    public static class ExtensionMethods
    {
        internal static Selector<T> GetSelector<T>(this Type type) => typeof(ISelector<T>).IsAssignableFrom(type)
            ? (Selector<T>) type.GetMethod("Select", BindingFlags.Instance | Public)
                .CreateDelegate(typeof(Selector<T>), null)
            : null;

        internal static Inserter<T> GetInserter<T>(this Type type) => typeof(IInserter<T>).IsAssignableFrom(type)
            ? (Inserter<T>) type.GetMethod("Insert", BindingFlags.Instance | Public)
                .CreateDelegate(typeof(Inserter<T>), null)
            : null;

        internal static Updater<T> GetUpdater<T>(this Type type) => typeof(IUpdater<T>).IsAssignableFrom(type)
            ? (Updater<T>) type.GetMethod("Update", BindingFlags.Instance | Public)
                .CreateDelegate(typeof(Updater<T>), null)
            : null;

        internal static Deleter<T> GetDeleter<T>(this Type type) => typeof(IDeleter<T>).IsAssignableFrom(type)
            ? (Deleter<T>) type.GetMethod("Delete", BindingFlags.Instance | Public)
                .CreateDelegate(typeof(Deleter<T>), null)
            : null;

        internal static IList<Type> GetConcreteSubclasses(this Type baseType) => baseType.GetSubclasses()
            .Where(type => !type.IsAbstract)
            .ToList();

        private static readonly MethodInfo ListGenerator = typeof(ExtensionMethods)
            .GetMethod(nameof(GenerateList), NonPublic | Static);

        private static List<T> GenerateList<T>(T thing) => new List<T> {thing};
        internal static string UriDecode(this string str) => HttpUtility.UrlDecode(str);
        internal static string UriEncode(this string str) => HttpUtility.UrlEncode(str);
        public static T GetReference<T>(this ulong? objectNo) where T : class => FromID(objectNo ?? 0) as T;
        public static T GetReference<T>(this ulong objectNo) where T : class => FromID(objectNo) as T;
        public static bool EqualsNoCase(this string s1, string s2) => string.Equals(s1, s2, CurrentCultureIgnoreCase);

        internal static dynamic MakeList(this object thing, Type resource) => ListGenerator
            .MakeGenericMethod(resource)
            .Invoke(null, new[] {thing});

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
                var matches = NameResources
                    .Where(pair => pair.Key.StartsWith(commonPart))
                    .Select(pair => pair.Value)
                    .Union(DB.All<ResourceAlias>()
                        .Where(alias => alias.Alias.StartsWith(commonPart))
                        .Select(alias => alias.GetResource()))
                    .ToList();
                if (matches.Any()) return matches;
                throw new UnknownResourceException(searchString);
            }
            var resource = ByAlias(searchString);
            if (resource == null)
                NameResources.TryGetValue(searchString, out resource);
            if (resource != null)
                return new[] {resource};
            throw new UnknownResourceException(searchString);
        }

        internal static IResource FindResource(this string searchString)
        {
            searchString = searchString.ToLower();
            var resource = ByAlias(searchString);
            if (resource == null)
                NameResources.TryGetValue(searchString, out resource);
            if (resource != null)
                return resource;
            var keys = NameResources.Keys
                .Where(key => key.EndsWith($".{searchString}"))
                .ToList();
            if (keys.Count < 1)
                throw new UnknownResourceException(searchString);
            if (keys.Count > 1)
                throw new AmbiguousResourceException(searchString,
                    keys.Select(k => NameResources[k].Name).ToList());
            return NameResources[keys.First()];
        }

        public static IEnumerable<T> Filter<T>(this IEnumerable<T> entities, Conditions filter)
        {
            return filter?.Apply((dynamic) entities) ?? entities;
        }

        internal static IEnumerable<T> Filter<T>(this IEnumerable<T> entities, IFilter filter)
        {
            return filter?.Apply((dynamic) entities) ?? entities;
        }

        internal static IEnumerable<dynamic> Process<T>(this IEnumerable<T> entities, IProcessor processor)
        {
            return processor?.Apply((dynamic) entities) ?? (IEnumerable<dynamic>) entities;
        }

        internal static IEnumerable<Type> GetSubclasses(this Type baseType) =>
            from assembly in AppDomain.CurrentDomain.GetAssemblies()
            from type in assembly.GetTypes()
            where type.IsSubclassOf(baseType)
            select type;

        internal static TAttribute GetAttribute<TAttribute>(this MemberInfo type) where TAttribute : Attribute =>
            type?.GetCustomAttributes<TAttribute>().FirstOrDefault();

        internal static bool HasAttribute<TAttribute>(this MemberInfo type)
            where TAttribute : Attribute => (type?.GetCustomAttributes<TAttribute>().Any()).GetValueOrDefault();

        internal static string RemoveTabsAndBreaks(this string input) => input != null
            ? Regex.Replace(input, @"\t|\n|\r", "")
            : null;

        internal static RESTarMethods[] ToMethodsArray(this string methodsString)
        {
            if (methodsString == null) return null;
            if (methodsString.Trim() == "*")
                return Methods;
            return methodsString.Split(',').Select(s => (RESTarMethods) Enum.Parse(typeof(RESTarMethods), s)).ToArray();
        }

        internal static string ToMethodsString(this IEnumerable<RESTarMethods> ie) => string.Join(", ", ie);

        public static byte[] ReadBytes(this Stream stream, int count)
        {
            var bytes = new byte[count];
            int read;
            if ((read = stream.Read(bytes, 0, count)) == count)
                return bytes;
            throw new ArgumentOutOfRangeException($"Count was {count}, read was {read}");
        }

        public static byte[] ReadRest(this Stream stream)
        {
            return stream.ReadBytes((int) (stream.Length - stream.Position));
        }

        public static T SafeGet<T>(this IDictionary<string, T> dict, string key)
        {
            return dict.ContainsKey(key) ? dict[key] : default(T);
        }

        public static T SafeGetNoCase<T>(this IDictionary<string, T> dict, string key)
        {
            var matches = dict.Where(pair => pair.Key.EqualsNoCase(key));
            return matches.Count() > 1 ? dict.SafeGet(key) : matches.FirstOrDefault().Value;
        }

        public static T SafeGetNoCase<T>(this IDictionary<string, T> dict, string key, out string actualKey)
        {
            var matches = dict.Where(pair => pair.Key.EqualsNoCase(key));
            if (matches.Count() > 1)
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

        public static T GetNoCase<T>(this IDictionary<string, T> dict, string key)
        {
            return dict.First(pair => pair.Key.EqualsNoCase(key)).Value;
        }

        public static Json ToViewModel(this DDictionary d)
        {
            var options = new Options(excludeNulls: true, includeInherited: true, dateFormat: ISO8601,
                unspecifiedDateTimeKindBehavior: _LocalTimes ? IsLocal : IsUTC);
            var dict = new Dictionary<string, dynamic>();
            d.KeyValuePairs.ForEach(pair => dict.Add(pair.Key + "$", pair.Value));
            return new Json(JSON.SerializeDynamic(dict, options));
        }

        public static Json ToJson(this Dictionary<string, dynamic> d, Options options)
        {
            var dict = new Dictionary<string, dynamic>();
            d.ForEach(pair => dict.Add(pair.Key + "$", pair.Value ?? ""));
            return new Json(JSON.SerializeDynamic(dict, options));
        }

        public static Dictionary<string, dynamic> ToTransient(this DDictionary d)
        {
            var dict = new Dictionary<string, dynamic>();
            d.KeyValuePairs.ForEach(pair => dict.Add(pair.Key, pair.Value));
            return dict;
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

        internal static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var e in source) action(e);
        }

        internal static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            var i = 0;
            foreach (var e in source) action(e, i++);
        }

        internal static Conditions ToConditions(this IEnumerable<Condition> conditions, Type resource)
        {
            if (conditions?.Any() != true) return null;
            var _conditions = new Conditions(resource);
            _conditions.AddRange(conditions);
            return _conditions;
        }

        internal static Select ToSelect(this IEnumerable<PropertyChain> props)
        {
            if (props?.Any() != true) return null;
            var _props = new Select();
            var propsGroups = props.GroupBy(p => p.Key);
            _props.AddRange(propsGroups.Select(g => g.First()));
            return _props;
        }

        internal static Add ToAdd(this IEnumerable<PropertyChain> props)
        {
            if (props?.Any() != true) return null;
            var _props = new Add();
            var propsGroups = props.GroupBy(p => p.Key);
            _props.AddRange(propsGroups.Select(g => g.First()));
            return _props;
        }

        internal static AccessRights ToAccessRights(this IEnumerable<AccessRight> accessRights)
        {
            var _accessRights = new AccessRights();
            foreach (var right in accessRights)
            {
                foreach (var resource in right.Resources)
                {
                    _accessRights[resource] = _accessRights.ContainsKey(resource)
                        ? _accessRights[resource].Union(right.AllowedMethods).ToList()
                        : right.AllowedMethods;
                }
            }
            return _accessRights;
        }

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
                message.Append(" : " + ie.Message);
                ie = ie.InnerException;
            }
            return message.ToString();
        }

        internal static string RESTarMemberName(this PropertyInfo propertyInfo)
        {
            return propertyInfo.GetAttribute<DataMemberAttribute>()?.Name ?? propertyInfo.Name;
        }

        internal static string MatchKey(this IDictionary<string, dynamic> dict, string key)
        {
            return dict.Keys.FirstOrDefault(k => key == k);
        }

        internal static string MatchKeyIgnoreCase(this IDictionary<string, dynamic> dict, string key)
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

        internal static string[] GetUniqueIdentifiers(this IResource resource)
        {
            var declared = resource.TargetType
                .GetPropertyList()
                .Where(prop => prop.HasAttribute<UniqueId>())
                .Select(prop => prop.RESTarMemberName())
                .ToArray();
            var objectId_objectNo = resource.IsStarcounterResource
                ? new[] {"ObjectID", "ObjectNo"}
                : new string[0];
            return declared.Union(objectId_objectNo).ToArray();
        }

        internal static Dictionary<string, dynamic> MakeDictionary(this object entity)
        {
            if (entity is DDictionary) return ((DDictionary) entity).ToTransient();
            if (entity is Dictionary<string, dynamic>) return (Dictionary<string, object>) entity;
            if (entity is IDictionary)
            {
                var dict = new Dictionary<string, dynamic>();
                foreach (DictionaryEntry pair in (IDictionary) entity)
                    dict[pair.Key.ToString()] = pair.Value;
                return dict;
            }
            return entity.GetType()
                .GetPropertyList()
                .ToDictionary(prop => prop.RESTarMemberName(),
                    prop => prop.GetValue(entity));
        }

        internal static Dictionary<string, dynamic> MakeTemplate(this IResource resouce) => Schema
            .MakeSchema(resouce.Name)
            .ToDictionary(
                pair => pair.Key,
                pair => Type.GetType(pair.Value).GetDefault()
            );

        internal static IEnumerable<Json> MakeViewModelJsonArray(this IEnumerable<dynamic> entities)
        {
            var options = new Options(excludeNulls: true, includeInherited: true, dateFormat: ISO8601,
                unspecifiedDateTimeKindBehavior: _LocalTimes ? IsLocal : IsUTC);
            var list = new List<Dictionary<string, dynamic>>();

            if (entities is IEnumerable<DDictionary>)
            {
                foreach (DDictionary entity in entities)
                {
                    var dict = new Dictionary<string, dynamic>();
                    entity.KeyValuePairs.ForEach(pair => dict.Add(pair.Key + "$", pair.Value ?? ""));
                    list.Add(dict);
                }
                return list.Select(dict => new Json(JSON.SerializeDynamic(dict, options)));
            }

            if (entities is IEnumerable<Dictionary<string, dynamic>>)
            {
                foreach (Dictionary<string, dynamic> entity in entities)
                {
                    var dict = new Dictionary<string, dynamic>();
                    entity.ForEach(pair => dict.Add(pair.Key + "$", pair.Value ?? ""));
                    list.Add(dict);
                }
                return list.Select(dict => new Json(JSON.SerializeDynamic(dict, options)));
            }

            if (entities is IEnumerable<IDictionary>)
            {
                foreach (IDictionary entity in entities)
                {
                    var dict = new Dictionary<string, dynamic>();
                    foreach (DictionaryEntry pair in entity)
                        dict[pair + "$"] = pair.Value ?? "";
                    list.Add(dict);
                }
                return list.Select(dict => new Json(JSON.SerializeDynamic(dict, options)));
            }

            return list.Select(dict => new Json(JSON.SerializeDynamic(dict, options)));
        }

        internal static PropertyInfo MatchProperty(this Type resource, string str, bool ignoreCase = true)
        {
            var matches = resource.GetPropertyList()
                .Where(p => string.Equals(str, p.RESTarMemberName(), ignoreCase
                    ? CurrentCultureIgnoreCase
                    : CurrentCulture));
            var count = matches.Count();
            if (count == 0) throw new UnknownColumnException(resource, str);
            if (count > 1)
            {
                if (!ignoreCase)
                    throw new AmbiguousColumnException(resource, str, matches.Select(m => m.RESTarMemberName()));
                return MatchProperty(resource, str, false);
            }
            return matches.First();
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

        internal static DataTable MakeTable(this IEnumerable<object> entities, IResource resource)
        {
            var table = new DataTable();
            var dicts = entities as IEnumerable<IDictionary<string, object>>;
            if (dicts != null)
            {
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
            }
            else
            {
                var properties = resource.TargetType.GetPropertyList();
                foreach (var propInfo in properties)
                {
                    var ColType = propInfo.PropertyType.IsClass && propInfo.PropertyType != typeof(string)
                        ? typeof(string)
                        : Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;
                    table.Columns.Add(propInfo.RESTarMemberName(), ColType);
                }
                foreach (var item in entities)
                {
                    var row = table.NewRow();
                    foreach (var propInfo in properties)
                    {
                        var key = propInfo.RESTarMemberName();
                        object value;
                        if (propInfo.HasAttribute<ExcelFlattenToString>())
                            value = propInfo.GetValue(item, null)?.ToString();
                        else value = propInfo.GetValue(item, null);
                        row.SetCellValue(key, value);
                    }
                    table.Rows.Add(row);
                }
            }
            return table;
        }

        private static void SetCellValue(this DataRow row, string name, dynamic value)
        {
            if (value == null)
            {
                row[name] = "";
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

        internal static string GetJsType(this Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    return typeof(IEnumerable).IsAssignableFrom(type)
                        ? "array"
                        : "object";
                case TypeCode.Boolean: return "boolean";
                case TypeCode.Char: return "string";
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
                case TypeCode.Decimal: return "number";
                case TypeCode.DateTime: return "datetime";
                case TypeCode.String: return "string";
                default: return null;
            }
        }

        static ExtensionMethods()
        {
            DEFAULT_MAKER = typeof(ExtensionMethods)
                .GetMethod(nameof(DEFAULT), NonPublic | Static);
        }

        internal static object GetDefault(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return DEFAULT_MAKER.MakeGenericMethod(type).Invoke(null, null);
        }

        private static readonly MethodInfo DEFAULT_MAKER;

        private static object DEFAULT<T>() => default(T);

        internal static bool IsSingular(this IEnumerable<object> ienum, Requests.Request request)
        {
            return request.Resource.Singleton || ienum?.Count() == 1 && !request.ResourceHome;
        }
    }
}