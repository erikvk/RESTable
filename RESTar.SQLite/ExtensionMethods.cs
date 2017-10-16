using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;

namespace RESTar.SQLite
{
    internal static class ExtensionMethods
    {
        internal static StaticProperty[] GetColumns(this IResource resource) => resource
            .GetStaticProperties()
            .Select(p => p.Value)
            .Where(p => p.HasAttribute<ColumnAttribute>())
            .ToArray();

        internal static string MakeCreateTableQuery(this IResource resource)
        {
            var columnProperties = resource
                .GetStaticProperties()
                .Select(pair => pair.Value)
                .Where(prop => prop.HasAttribute<ColumnAttribute>())
                .Select(prop => $"{prop.Name} {prop.Type.ToSQLType(resource)}");
            return $"CREATE TABLE IF NOT EXISTS {resource.GetSQLiteTableName()} ({string.Join(" , ", columnProperties)})";
        }

        internal static string GetSQLiteTableName(this IResource resource) => resource.Name.Replace('.', '_');

        internal static string ToSQLType(this Type type, IResource resource)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64: return "INTEGER";
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal: return "REAL";
                case TypeCode.Char:
                case TypeCode.String: return "TEXT";
                case TypeCode.Boolean: return "BOOL";
                case TypeCode.DateTime: return "DATETIME";
                default:
                    throw new SQLiteException("Could not create SQLite database column for a property " +
                                              $"of type '{type.FullName}' in resource '{resource.Name}'");
            }
        }

        private static string MakeSQLValueLiteral(this object o)
        {
            switch (o)
            {
                case DateTime _: return $"\'{o:O}\'";
                case string _: return $"\'{o}\'";
                default: return $"{o}";
            }
        }

        internal static string ToSQLiteWhereClause<T>(this IEnumerable<Condition<T>> dbConditions) where T : class
        {
            var clause = string.Join(" AND ", dbConditions
                .Select(s => $"{s.Key} {s.Operator.SQL} {MakeSQLValueLiteral((object) s.Value)}"));
            return string.IsNullOrWhiteSpace(clause) ? null : "WHERE " + clause;
        }

        internal static string ToSQLiteInsertInto<T>(this T entity, StaticProperty[] columns) where T : class
        {
            return string.Join(",", columns.Select(c => MakeSQLValueLiteral((object) c.GetValue(entity))));
        }
    }
}