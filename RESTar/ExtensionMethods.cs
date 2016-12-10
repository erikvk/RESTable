using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Jil;

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
                return JSON.SerializeDynamic(obj, Options.ISO8601PrettyPrintExcludeNullsIncludeInherited);
            return JSON.SerializeDynamic(obj, Options.ISO8601ExcludeNullsIncludeInherited);
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
                        Options.ISO8601PrettyPrintExcludeNullsIncludeInherited
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
                    Options.ISO8601ExcludeNullsIncludeInherited
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