using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Jil;
using Starcounter;

namespace RESTar
{
    internal static class ExtensionMethods
    {
        internal static Type FindResource(this string searchString)
        {
            searchString = searchString.ToLower();
            Type type;
            RESTarConfig.ResourcesDict.TryGetValue(searchString, out type);
            if (type != null)
                return type;
            var keys = RESTarConfig.ResourcesDict
                .Keys
                .Where(key => key.EndsWith(searchString))
                .ToList();
            if (keys.Count < 1)
                throw new UnknownResourceException(searchString);
            if (keys.Count > 1)
                throw new AmbiguousResourceException(searchString,
                    keys.Select(k => RESTarConfig.ResourcesDict[k].FullName).ToList());
            return RESTarConfig.ResourcesDict[keys.First()];
        }

        internal static string FindLastKeyValue(Type resource, string keyString, dynamic root, out dynamic value)
        {
            value = null;
            keyString = keyString.ToLower();
            var parts = keyString.Split('.');
            if (parts.Length == 1)
                throw new SyntaxException($"Invalid condition '{keyString}'");
            var types = new List<Type>();
            var names = new List<string>();
            var first = true;
            foreach (var str in parts.Take(parts.Length - 1))
            {
                var containingType = types.LastOrDefault() ?? resource;
                var column = containingType
                    .GetProperties()
                    .FirstOrDefault(prop => str == prop.Name.ToLower());
                if (column == null)
                    throw new UnknownColumnException(resource, keyString);
                var type = column.PropertyType;
                if (type.GetAttribute<RESTarAttribute>()?.AvailableMethods.Contains(RESTarMethods.GET) != true)
                    throw new SyntaxException($"RESTar does not have read access to resource '{type.FullName}' " +
                                              $"referenced in '{keyString}'.");
                if (!type.HasAttribute<DatabaseAttribute>())
                    throw new SyntaxException($"A part '{str}' of condition key '{keyString}' referenced type " +
                                              $"'{type.FullName}', which is of a non-database type. Only references " +
                                              "to database types (resources) can be used in queries.");
                if (first)
                    value = column.GetValue(root);
                else if (value != null)
                    value = column.GetValue(value);
                types.Add(type);
                names.Add(column.Name);
                first = false;
            }
            var lastType = types.Last();
            var lastColumns = lastType.GetColumns();
            var lastColumn = lastColumns.FindColumn(lastType, parts.Last());
            if (value != null)
                value = lastColumn.GetValue(value);
            names.Add(lastColumn.Name);
            return string.Join(".", names);
        }

        internal static PropertyInfo FindColumn(this IEnumerable<PropertyInfo> columns, Type resource, string str)
        {
            var matches = columns.Where(p => str == (p.GetAttribute<DataMemberAttribute>()?.Name?.ToLower()
                                                     ?? p.Name.ToLower()));
            if (matches.Count() == 1)
                return matches.First();
            if (matches.Count() > 1)
                throw new AmbiguousColumnException(resource, str, matches.Select(m => m.Name).ToList());
            throw new UnknownColumnException(resource, str);
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

        internal static string SerializeDyn(this object obj)
        {
            if (Settings.Instance.PrettyPrint)
                return JSON.SerializeDynamic(obj, Options.ISO8601PrettyPrintIncludeInherited);
            return JSON.SerializeDynamic(obj, Options.ISO8601IncludeInherited);
        }

        internal static string Serialize(this object obj, Type resource)
        {
            var method = typeof(JSON).GetMethods().First(n => n.Name == "Serialize" && n.ReturnType == typeof(void));
            var generic = method.MakeGenericMethod(resource);
            var writer = new StringWriter();
            if (Settings.Instance.PrettyPrint)
            {
                generic.Invoke
                (
                    null,
                    new[]
                    {
                        obj,
                        writer,
                        Options.ISO8601PrettyPrintIncludeInherited
                    }
                );
                return writer.ToString();
            }
            generic.Invoke
            (
                null,
                new[]
                {
                    obj,
                    writer,
                    Options.ISO8601IncludeInherited
                }
            );
            return writer.ToString();
        }

        internal static string[] Keys(this IEnumerable<Condition> conditions)
        {
            return conditions.Select(c => c.Key).ToArray();
        }

        internal static WhereClause ToWhereClause(this IList<Condition> conditions)
        {
            if (conditions == null)
                return null;

            var stringPart = new List<string>();
            var valuesPart = new List<object>();

            foreach (var c in conditions)
            {
                if (c.Value == null)
                    stringPart.Add($"t.{c.Key} {(c.Operator.Common == "!=" ? " IS NOT NULL " : " IS NULL ")}");
                else
                {
                    stringPart.Add($"t.{c.Key} {c.Operator.SQL}?");
                    valuesPart.Add(c.Value);
                }
            }

            return new WhereClause
            {
                stringPart = $"WHERE {string.Join(" AND ", stringPart)}",
                valuesPart = valuesPart.ToArray()
            };
        }

        public static object ValueForEquals(this IEnumerable<Condition> conditions, string key)
        {
            return conditions?.FirstOrDefault(c => c.Operator.Common == "=" && c.Key == key.ToLower())?.Value;
        }

        public static object ValueForNotEquals(this IEnumerable<Condition> conditions, string key)
        {
            return conditions?.FirstOrDefault(c => c.Operator.Common == "!=" && c.Key == key.ToLower())?.Value;
        }

        public static object ValueForGreaterThan(this IEnumerable<Condition> conditions, string key)
        {
            return conditions?.FirstOrDefault(c => c.Operator.Common == ">" && c.Key == key.ToLower())?.Value;
        }

        public static object ValueForLessThan(this IEnumerable<Condition> conditions, string key)
        {
            return conditions?.FirstOrDefault(c => c.Operator.Common == "<" && c.Key == key.ToLower())?.Value;
        }

        public static object ValueForGreaterThanOrEquals(this IEnumerable<Condition> conditions, string key)
        {
            return conditions?.FirstOrDefault(c => c.Operator.Common == ">=" && c.Key == key.ToLower())?.Value;
        }

        public static object ValueForLessThanOrEquals(this IEnumerable<Condition> conditions, string key)
        {
            return conditions?.FirstOrDefault(c => c.Operator.Common == "<=" && c.Key == key.ToLower())?.Value;
        }

        internal static object[] Values(this IEnumerable<Condition> conditions)
        {
            return conditions.Select(c => c.Value).ToArray();
        }

        internal static Operator[] Operators(this IEnumerable<Condition> conditions)
        {
            return conditions.Select(c => c.Operator).ToArray();
        }

        internal static ICollection<RESTarMethods> AvailableMethods(this Type resource)
        {
            return resource.GetAttribute<RESTarAttribute>()?.AvailableMethods;
        }

        internal static string ToMethodsString(this IEnumerable<RESTarMethods> ie) => string.Join(", ", ie);
    }
}