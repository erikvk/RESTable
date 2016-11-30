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
    internal static class Common
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

        internal static TAttribute GetAttribute<TAttribute>(this Type type) where TAttribute : Attribute
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

        internal static IEnumerable<object> GetFromDb(Type resource, string[] select, WhereClause whereClause,
            int limit = -1, OrderBy orderBy = null)
        {
            var sql = $"SELECT t FROM {resource.FullName} t {whereClause?.stringPart} {orderBy?.SQL}";
            IEnumerable<object> entities;
            if (limit < 1)
                entities = Db.SQL(sql, whereClause?.valuesPart);
            else if (limit == 1)
                entities = new[] {Db.SQL(sql, whereClause?.valuesPart).First};
            else entities = Db.SQL(sql, whereClause?.valuesPart).Take(limit);

            if (select == null)
                return entities;

            return entities.Select(o =>
            {
                var props = new List<PropertyInfo>();
                foreach (var s in select)
                {
                    var matches = o.GetType().GetProperties().Where(p => s == p.Name.ToLower()).ToList();
                    if (matches.Count == 1)
                        props.Add(matches.First());
                    else if (matches.Count > 1)
                        throw new AmbiguousColumnException(resource, s, matches.Select(m => m.Name).ToList());
                    else if (matches.Count < 1)
                        throw new UnknownColumnException(resource, s);
                }
                return props.ToDictionary(p => p.Name, p => p.GetValue(o));
            });
        }

        internal static string Serialize(this object o)
        {
            if (Settings.Instance.PrettyPrint)
                return JSON.SerializeDynamic(o, Options.ISO8601PrettyPrintExcludeNullsIncludeInherited);
            return JSON.SerializeDynamic(o, Options.ISO8601ExcludeNullsIncludeInherited);
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