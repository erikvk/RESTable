using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Jil;
using Starcounter;

namespace RESTar
{
    internal static class Common
    {
        internal static Type FindResource(this string typeName)
        {
            typeName = typeName.ToLower();
            Type type;
            Config.DbDomainDict.TryGetValue(typeName, out type);
            if (type != null)
                return type;
            var keys = Config.DbDomainDict
                .Keys
                .Where(key => key.EndsWith(typeName))
                .ToList();
            if (keys.Count > 1)
                throw new UnknownResourceException(typeName, keys.Select(k => Config.DbDomainDict[k].FullName).ToList());
            return Config.DbDomainDict[keys.First()];
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

        internal static IEnumerable<object> GetFromDb(Type resource, WhereClause whereClause, int limit = -1,
            OrderBy orderBy = null)
        {
            var sql = $"SELECT t FROM {resource.FullName} t {whereClause?.stringPart} {orderBy?.SQL}";
            if (limit < 1) return Db.SQL(sql, whereClause?.valuesPart);
            if (limit == 1) return new[] {Db.SQL(sql, whereClause?.valuesPart).First};
            return Db.SQL(sql, whereClause?.valuesPart).Take(limit);
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
            return Config.Methods.Except(resource.AvailableMethods() ?? new RESTarMethods[0]);
        }

        public static void ParallelForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var e in source)
                Scheduling.ScheduleTask(() => action(e));
        }

        internal static string ToMethodsString(this IEnumerable<RESTarMethods> ie) => string.Join(", ", ie);
    }
}