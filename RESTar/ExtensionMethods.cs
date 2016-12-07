using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        internal static PropertyInfo FindColumn(this string searchString, Type resource)
        {
            var matches = resource
                .GetProperties()
                .Where(p => p.GetAttribute<IgnoreDataMemberAttribute>() == null)
                .Where(p =>
                {
                    var name = p.GetAttribute<DataMemberAttribute>()?.Name?.ToLower()
                               ?? p.Name.ToLower();
                    return searchString == name;
                });

            if (matches.Count() == 1)
                return matches.First();
            if (matches.Count() > 1)
                throw new AmbiguousColumnException(resource, searchString, matches.Select(m => m.Name).ToList());
            throw new UnknownColumnException(resource, searchString);
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
            var method = typeof(JSON).GetMethods().First(n => n.Name == "Serialize" && n.GetParameters().Length == 2);
            var generic = method.MakeGenericMethod(resource);
            if (Settings.Instance.PrettyPrint)
                return (string) generic.Invoke
                (
                    null,
                    new[]
                    {
                        obj,
                        Options.ISO8601PrettyPrintExcludeNullsIncludeInherited
                    }
                );
            return (string) generic.Invoke
            (
                null,
                new[]
                {
                    obj,
                    Options.ISO8601ExcludeNullsIncludeInherited
                }
            );
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

        internal static IEnumerable<RESTarMethods> AvailableMethods(this Type resource)
        {
            return resource.GetAttribute<RESTarAttribute>()?.AvailableMethods;
        }

        internal static IEnumerable<RESTarMethods> BlockedMethods(this Type resource)
        {
            return RESTarConfig.Methods.Except(resource.AvailableMethods() ?? new RESTarMethods[0]);
        }

        internal static string ToMethodsString(this IEnumerable<RESTarMethods> ie) => string.Join(", ", ie);
    }
}