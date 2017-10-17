using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using static RESTar.Operators;

namespace RESTar.SQLite
{
    internal static class ExtensionMethods
    {
        internal static Dictionary<string, StaticProperty> GetColumns(this IResource resource) => resource
            .GetStaticProperties()
            .Where(p => p.Value.HasAttribute<ColumnAttribute>())
            .ToDictionary(p => p.Key, p => p.Value);

        internal static string GetColumnDef(this StaticProperty column) => $"{column.Name.ToLower()} {column.Type.ToSQLType()}";

        internal static string GetSQLiteTableName(this IResource resource) => resource.Name.Replace('.', '_');

        internal static string GetResourceName(this string sqliteTableName) =>
            Resource.SafeGet(sqliteTableName.Replace('_', '.')).Name;

        internal static bool IsSQLiteCompatibleValueType(this Type type, Type resourceType, out string error)
        {
            if (type.ToSQLType() == null)
            {
                error = "Could not create SQLite database column for a property " +
                        $"of type '{type.FullName}' in resource type " +
                        $"'{resourceType?.FullName}'";
                return false;
            }
            error = null;
            return true;
        }

        internal static bool IsNullable(this Type type, out Type baseType)
        {
            baseType = null;
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Nullable<>))
                return false;
            baseType = type.GenericTypeArguments[0];
            return true;
        }

        internal static (string, string) TSplit(this string str, char splitCharacter)
        {
            var split = str.Split(splitCharacter);

            return (split[0], split.ElementAtOrDefault(1));
        }

        internal static string ToSQLType(this Type type)
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
                case var _ when type.IsNullable(out var baseType):
                    return baseType.ToSQLType();
                default: return null;
            }
        }

        private static string MakeSQLValueLiteral(this object o)
        {
            switch (o)
            {
                case null: return "NULL";
                case DateTime _: return $"\'{o:O}\'";
                case string _: return $"\'{o}\'";
                default: return $"{o}";
            }
        }

        internal static string ToSQLiteWhereClause<T>(this IEnumerable<Condition<T>> dbConditions) where T : class
        {
            var values = string.Join(" AND ", dbConditions.Select(condition =>
            {
                var op = condition.Operator.SQL;
                var valueLiteral = MakeSQLValueLiteral((object) condition.Value);
                if (valueLiteral == "NULL")
                {
                    switch (condition.Operator.OpCode)
                    {
                        case EQUALS:
                            op = "IS";
                            break;
                        case NOT_EQUALS:
                            op = "IS NOT";
                            break;
                        default: throw new SQLiteException($"Operator '{op}' is not valid for comparison with NULL");
                    }
                }
                return $"{condition.Key} {op} {valueLiteral}";
            }));
            return string.IsNullOrWhiteSpace(values) ? null : "WHERE " + values;
        }

        internal static string ToSQLiteInsertInto<T>(this T entity, IEnumerable<StaticProperty> columns) where T : class
        {
            return string.Join(",", columns.Select(c => MakeSQLValueLiteral((object) c.GetValue(entity))));
        }
    }
}