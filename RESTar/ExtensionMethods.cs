using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Dynamit;
using RESTar.Auth;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;
using static System.Reflection.BindingFlags;
using static System.StringComparison;
using static RESTar.ResourceAlias;
using static RESTar.RESTarConfig;
using static RESTar.RESTarMethods;
using static Starcounter.DbHelper;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    public static class ExtensionMethods
    {
        internal static Selector<T> GetSelector<T>(this Type type) => typeof(ISelector<T>).IsAssignableFrom(type)
            ? (Selector<T>) type.GetMethod("Select", Instance | Public).CreateDelegate(typeof(Selector<T>), null)
            : null;

        internal static Inserter<T> GetInserter<T>(this Type type) => typeof(IInserter<T>).IsAssignableFrom(type)
            ? (Inserter<T>) type.GetMethod("Insert", Instance | Public).CreateDelegate(typeof(Inserter<T>), null)
            : null;

        internal static Updater<T> GetUpdater<T>(this Type type) => typeof(IUpdater<T>).IsAssignableFrom(type)
            ? (Updater<T>) type.GetMethod("Update", Instance | Public).CreateDelegate(typeof(Updater<T>), null)
            : null;

        internal static Deleter<T> GetDeleter<T>(this Type type) => typeof(IDeleter<T>).IsAssignableFrom(type)
            ? (Deleter<T>) type.GetMethod("Delete", Instance | Public).CreateDelegate(typeof(Deleter<T>), null)
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
            return filter?.Apply(entities) ?? entities;
        }

        internal static IEnumerable<T> Filter<T>(this IEnumerable<T> entities, IFilter filter)
        {
            return filter?.Apply(entities) ?? entities;
        }

        internal static IEnumerable<T> Filter<T>(this IEnumerable<T> entities, IFilter[] filters)
        {
            return filters?.Any() == true ? filters.Aggregate(entities, (e, f) => f?.Apply(e) ?? e) : entities;
        }

        internal static IEnumerable<dynamic> Process<T>(this IEnumerable<T> entities, IProcessor processor)
        {
            return processor?.Apply(entities) ?? (IEnumerable<dynamic>) entities;
        }

        internal static IEnumerable<dynamic> Process<T>(this IEnumerable<T> entities, IProcessor[] processors)
        {
            if (processors?.Any() != true)
                return (IEnumerable<dynamic>) entities;
            IEnumerable<dynamic> results = null;
            processors.ForEach(processor =>
            {
                if (processor == null) return;
                results = results == null
                    ? processor.Apply(entities)
                    : processor.Apply(results);
            });
            return results;
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

        public static T GetNoCase<T>(this IDictionary<string, T> dict, string key)
        {
            return dict.First(pair => pair.Key.EqualsNoCase(key)).Value;
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

        internal static Conditions ToConditions(this IEnumerable<Condition> conditions, IResource resource)
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
            _props.AddRange(props);
            return _props;
        }

        internal static Add ToAdd(this IEnumerable<PropertyChain> props)
        {
            if (props?.Any() != true) return null;
            var _props = new Add();
            _props.AddRange(props);
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

        internal static Type ExpectedType(this RESTarMetaConditions condition)
        {
            switch (condition)
            {
                case RESTarMetaConditions.Limit: return typeof(int);
                case RESTarMetaConditions.Order_desc: return typeof(string);
                case RESTarMetaConditions.Order_asc: return typeof(string);
                case RESTarMetaConditions.Unsafe: return typeof(bool);
                case RESTarMetaConditions.Select: return typeof(string);
                case RESTarMetaConditions.Add: return typeof(string);
                case RESTarMetaConditions.Rename: return typeof(string);
                case RESTarMetaConditions.Dynamic: return typeof(bool);
                case RESTarMetaConditions.Safepost: return typeof(string);
                default: throw new ArgumentOutOfRangeException(nameof(condition), condition, null);
            }
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

        internal static bool ContainsKeyIgnorecase(this IDictionary<string, dynamic> dict, string key,
            out string actualKey)
        {
            string _actualKey = null;
            var results = dict.Keys.FirstOrDefault(k =>
            {
                var equals = k.EqualsNoCase(key);
                if (equals) _actualKey = k;
                return equals;
            });
            actualKey = _actualKey;
            return results != null;
        }

        internal static Dictionary<string, dynamic> MakeDictionary(this object entity)
        {
            if (entity is DDictionary) return ((DDictionary) entity).ToTransient();
            if (entity is Dictionary<string, dynamic>) return (Dictionary<string, object>) entity;
            return entity.GetType()
                .GetPropertyList()
                .ToDictionary(prop => prop.RESTarMemberName(),
                    prop => prop.GetValue(entity));
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
    }
}