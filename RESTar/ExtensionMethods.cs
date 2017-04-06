using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Starcounter;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    public static class ExtensionMethods
    {
        internal static string UriDecode(this string str) => HttpUtility.UrlDecode(str);
        internal static string UriEncode(this string str) => HttpUtility.UrlEncode(str);

        internal static Selector<T> GetSelector<T>(this Type type)
        {
            if (!typeof(ISelector<T>).IsAssignableFrom(type)) return null;
            return new Selector<T>((Func<IRequest, IEnumerable<T>>)
                type.GetMethod("Select", BindingFlags.Instance | BindingFlags.Public)
                    .CreateDelegate(typeof(Func<IRequest, IEnumerable<T>>), null)
            );
        }

        internal static Inserter<T> GetInserter<T>(this Type type)
        {
            if (!typeof(IInserter<T>).IsAssignableFrom(type)) return null;
            return new Inserter<T>((Func<IEnumerable<T>, IRequest, int>)
                type.GetMethod("Insert", BindingFlags.Instance | BindingFlags.Public)
                    .CreateDelegate(typeof(Func<IEnumerable<T>, IRequest, int>), null)
            );
        }

        internal static Updater<T> GetUpdater<T>(this Type type)
        {
            if (!typeof(IUpdater<T>).IsAssignableFrom(type)) return null;
            return new Updater<T>((Func<IEnumerable<T>, IRequest, int>)
                type.GetMethod("Update", BindingFlags.Instance | BindingFlags.Public)
                    .CreateDelegate(typeof(Func<IEnumerable<T>, IRequest, int>), null)
            );
        }

        internal static Deleter<T> GetDeleter<T>(this Type type)
        {
            if (!typeof(IDeleter<T>).IsAssignableFrom(type)) return null;
            return new Deleter<T>((Func<IEnumerable<T>, IRequest, int>)
                type.GetMethod("Delete", BindingFlags.Instance | BindingFlags.Public)
                    .CreateDelegate(typeof(Func<IEnumerable<T>, IRequest, int>), null)
            );
        }

        internal static IList<Type> GetConcreteSubclasses(this Type baseType)
        {
            return baseType.GetSubclasses().Where(type => !type.IsAbstract).ToList();
        }

        private static readonly MethodInfo ListGenerator = typeof(ExtensionMethods).GetMethod("GenerateList",
            BindingFlags.NonPublic | BindingFlags.Static);

        private static List<T> GenerateList<T>(T thing) => new List<T> {thing};

        internal static dynamic MakeList(this object thing, Type resource)
        {
            return ListGenerator.MakeGenericMethod(resource).Invoke(null, new[] {thing});
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
                var matches = RESTarConfig.NameResources
                    .Where(pair => pair.Key.StartsWith(commonPart))
                    .Select(pair => pair.Value)
                    .Union(DB.All<ResourceAlias>()
                        .Where(alias => alias.Alias.StartsWith(commonPart))
                        .Select(alias => alias.GetResource()))
                    .ToList();
                if (matches.Any())
                    return matches;
                throw new UnknownResourceException(searchString);
            }
            var resource = ResourceAlias.ByAlias(searchString);
            if (resource == null)
                RESTarConfig.NameResources.TryGetValue(searchString, out resource);
            if (resource != null)
                return new[] {resource};
            throw new UnknownResourceException(searchString);
        }

        internal static IResource FindResource(this string searchString)
        {
            searchString = searchString.ToLower();
            var resource = ResourceAlias.ByAlias(searchString);
            if (resource == null)
                RESTarConfig.NameResources.TryGetValue(searchString, out resource);
            if (resource != null)
                return resource;
            var keys = RESTarConfig.NameResources
                .Keys
                .Where(key => key.EndsWith($".{searchString}"))
                .ToList();
            if (keys.Count < 1)
                throw new UnknownResourceException(searchString);
            if (keys.Count > 1)
                throw new AmbiguousResourceException(searchString,
                    keys.Select(k => RESTarConfig.NameResources[k].Name).ToList());
            return RESTarConfig.NameResources[keys.First()];
        }

        internal static string GetValueFromKeyString(Type resource, string keyString, dynamic root, out dynamic value)
        {
            value = null;
            keyString = keyString.ToLower();
            var parts = keyString.Split('.');
            if (parts.Length == 1)
            {
                var column = resource.GetColumns().FindColumn(resource, keyString);
                value = column.GetValue(root);
                return column.Name;
            }
            var types = new List<Type>();
            var names = new List<string>();
            var first = true;
            foreach (var str in parts.Take(parts.Length - 1))
            {
                var containingType = types.LastOrDefault() ?? resource;
                var column = containingType.GetProperties().FirstOrDefault(prop => str == prop.Name.ToLower());
                if (column == null)
                    throw new UnknownColumnException(resource, keyString);
                var type = column.PropertyType;
                if (first)
                    value = column.GetValue(root);
                else if (value != null)
                    value = column.GetValue(value);
                types.Add(type);
                names.Add(column.Name);
                first = false;
            }
            if (parts.Last() == "objectno")
            {
                if (value != null)
                    value = DbHelper.GetObjectNo(value);
                names.Add("ObjectNo");
            }
            else if (parts.Last() == "objectid")
            {
                if (value != null)
                    value = DbHelper.GetObjectID(value);
                names.Add("ObjectID");
            }
            else
            {
                var lastType = types.Last();
                var lastColumns = lastType.GetColumns();
                var lastColumn = lastColumns.FindColumn(lastType, parts.Last());
                if (value != null)
                    value = lastColumn.GetValue(value);
                names.Add(lastColumn.Name);
            }
            return string.Join(".", names);
        }

        internal static PropertyInfo FindColumn(this IEnumerable<PropertyInfo> columns, Type resource, string str)
        {
            var matches = columns.Where(p => str.ToLower() == (p.GetAttribute<DataMemberAttribute>()?.Name?.ToLower()
                                                               ?? p.Name.ToLower())).ToList();
            var count = matches.Count;
            if (count < 1) throw new UnknownColumnException(resource, str);
            if (count > 1) throw new AmbiguousColumnException(resource, str, matches.Select(m => m.Name).ToList());
            return matches.First();
        }

        internal static string GetColumnName(this PropertyInfo column)
        {
            return column.GetAttribute<DataMemberAttribute>()?.Name ?? column.Name;
        }

        internal static PropertyInfo[] GetColumns(this Type resource)
        {
            return resource.GetProperties().Where(p => !p.HasAttribute<IgnoreDataMemberAttribute>()).ToArray();
        }

        internal static IEnumerable<Type> GetSubclasses(this Type baseType)
        {
            return from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                where type.IsSubclassOf(baseType)
                select type;
        }

        internal static string Capitalize(this string str)
        {
            var lower = str.ToLower();
            return lower.Substring(0, 1).ToUpper() + lower.Substring(1);
        }

        internal static TAttribute GetAttribute<TAttribute>(this MemberInfo type) where TAttribute : Attribute
        {
            return type?.GetCustomAttributes<TAttribute>().FirstOrDefault();
        }

        internal static bool HasAttribute<TAttribute>(this MemberInfo type) where TAttribute : Attribute
        {
            return (type?.GetCustomAttributes<TAttribute>().Any()).GetValueOrDefault();
        }

        internal static string RemoveTabsAndBreaks(this string input)
        {
            return input != null ? Regex.Replace(input, @"\t|\n|\r", "") : null;
        }

        internal static WhereClause ToDDictWhereClause(this Condition condition)
        {
            if (condition == null)
                return null;

            string stringPart;
            object[] valuePart = null;

            if (condition.Value == null)
                stringPart = "WHERE t.Key =? AND t.Value " +
                             $"{(condition.Operator == Operators.NOT_EQUALS ? "IS NOT NULL" : "IS NULL")}";
            else
            {
                stringPart = $"t.Key ?= AND t.Value {condition.Operator.SQL}?";
                valuePart = new object[] {condition.Key, condition.Value};
            }
            return new WhereClause
            {
                stringPart = stringPart,
                valuesPart = valuePart
            };
        }

        public static WhereClause ToWhereClause(this ICollection<Condition> conditions)
        {
            if (conditions == null)
                return null;
            if (!conditions.Any())
                return new WhereClause();

            var stringPart = new List<string>();
            var valuesPart = new List<object>();

            foreach (var c in conditions)
            {
                if (c.Value == null)
                    stringPart.Add(
                        $"t.{c.Key.Fnuttify()} {(c.Operator == Operators.NOT_EQUALS ? " IS NOT NULL " : " IS NULL ")}");
                else
                {
                    stringPart.Add($"t.{c.Key.Fnuttify()} {c.Operator.SQL}?");
                    valuesPart.Add(c.Value);
                }
            }

            return new WhereClause
            {
                stringPart = $"WHERE {string.Join(" AND ", stringPart)}",
                valuesPart = valuesPart.ToArray()
            };
        }

        public static T GetReference<T>(this ulong? objectNo) where T : class
        {
            return DbHelper.FromID(objectNo.GetValueOrDefault()) as T;
        }

        public static T GetReference<T>(this ulong objectNo) where T : class
        {
            return DbHelper.FromID(objectNo) as T;
        }

        internal static ICollection<RESTarMethods> ToMethodsList(this string methodsString)
        {
            if (methodsString == null) return null;
            if (methodsString.Trim() == "*")
                return RESTarConfig.Methods;
            return methodsString.Split(',').Select(s => (RESTarMethods) Enum.Parse(typeof(RESTarMethods), s)).ToList();
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

        public static dynamic SafeGetNoCase(this IDictionary<string, dynamic> dict, string key)
        {
            return dict.FirstOrDefault(
                pair => string.Equals(pair.Key, key, StringComparison.CurrentCultureIgnoreCase)
            ).Value;
        }

        public static dynamic GetNoCase(this IDictionary<string, dynamic> dict, string key)
        {
            return dict.First(
                pair => string.Equals(pair.Key, key, StringComparison.CurrentCultureIgnoreCase)
            ).Value;
        }

        internal static RESTarMethods[] ToMethods(this RESTarPresets preset)
        {
            switch (preset)
            {
                case RESTarPresets.ReadOnly:
                    return new[]
                    {
                        RESTarMethods.GET
                    };
                case RESTarPresets.WriteOnly:
                    return new[]
                    {
                        RESTarMethods.POST,
                        RESTarMethods.DELETE
                    };
                case RESTarPresets.ReadAndUpdate:
                    return new[]
                    {
                        RESTarMethods.GET,
                        RESTarMethods.PATCH
                    };
                case RESTarPresets.ReadAndWrite:
                    return RESTarConfig.Methods;
            }
            throw new ArgumentOutOfRangeException(nameof(preset));
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

        internal static int Count(this IResource resource, IRequest request)
        {
            return resource.Select(request).Count();
        }

        internal static Conditions ToConditions(this IEnumerable<Condition> conditions)
        {
            var _conditions = new Conditions();
            _conditions.AddRange(conditions);
            return _conditions;
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
                case RESTarMetaConditions.Limit:
                    return typeof(int);
                case RESTarMetaConditions.Order_desc:
                    return typeof(string);
                case RESTarMetaConditions.Order_asc:
                    return typeof(string);
                case RESTarMetaConditions.Unsafe:
                    return typeof(bool);
                case RESTarMetaConditions.Select:
                    return typeof(string);
                case RESTarMetaConditions.Rename:
                    return typeof(string);
                case RESTarMetaConditions.Dynamic:
                    return typeof(bool);
                case RESTarMetaConditions.Map:
                    return typeof(string);
                case RESTarMetaConditions.Safepost:
                    return typeof(string);
                default:
                    throw new ArgumentOutOfRangeException(nameof(condition), condition, null);
            }
        }

        public static byte[] ToBytes(this string json) => Encoding.UTF8.GetBytes(json);

        public static string TotalStackTrace(this Exception e)
        {
            var stacktrace = new StringBuilder(e.StackTrace);
            var ie = e.InnerException;
            while (ie != null)
            {
                stacktrace.Insert(0, ie.StackTrace + " | ");
                ie = ie.InnerException;
            }
            return stacktrace.ToString();
        }

        public static string TotalMessage(this Exception e)
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
    }
}