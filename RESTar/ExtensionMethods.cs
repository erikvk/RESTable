using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Jil;
using Starcounter;
using Starcounter.Query.Execution;

namespace RESTar
{
    internal static class ExtensionMethods
    {
        internal static Type FindResource(this string searchString)
        {
            searchString = searchString.ToLower();
            Type type;
            RESTarConfig.DbDomainDict.TryGetValue(searchString, out type);
            if (type != null)
                return type;
            var keys = RESTarConfig.DbDomainDict
                .Keys
                .Where(key => key.EndsWith(searchString))
                .ToList();
            if (keys.Count < 1)
                throw new UnknownResourceException(searchString);
            if (keys.Count > 1)
                throw new AmbiguousResourceException(searchString,
                    keys.Select(k => RESTarConfig.DbDomainDict[k].FullName).ToList());
            return RESTarConfig.DbDomainDict[keys.First()];
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
            return new WhereClause
            {
                stringPart = $"WHERE {string.Join(" AND ", conditions.Select(c => $"t.{c.Key} {c.Operator.SQL}?"))}",
                valuesPart = conditions.Values()
            };
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

        public static void ParallelForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var e in source)
                Scheduling.ScheduleTask(() => action(e));
        }

        internal static string ToMethodsString(this IEnumerable<RESTarMethods> ie) => string.Join(", ", ie);
    }
}