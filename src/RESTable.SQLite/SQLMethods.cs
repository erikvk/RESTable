using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using RESTable.Requests;
using static RESTable.Requests.Operators;

namespace RESTable.Sqlite
{
    internal static class SqlMethods
    {
        internal static string Fnuttify(this string sqlKey) => $"\"{sqlKey}\"";

        internal static bool IsSqliteCompatibleValueType(this Type type) => type.ResolveClrTypeCode() != CLRDataType.Unsupported;

        internal static CLRDataType ResolveClrTypeCode(this Type type)
        {
            if (type.IsNullable(out var baseType))
                type = baseType!;
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Int16 => CLRDataType.Int16,
                TypeCode.Int32 => CLRDataType.Int32,
                TypeCode.Int64 => CLRDataType.Int64,
                TypeCode.Single => CLRDataType.Single,
                TypeCode.Double => CLRDataType.Double,
                TypeCode.Decimal => CLRDataType.Decimal,
                TypeCode.Byte => CLRDataType.Byte,
                TypeCode.String => CLRDataType.String,
                TypeCode.Boolean => CLRDataType.Boolean,
                TypeCode.DateTime => CLRDataType.DateTime,
                _ => CLRDataType.Unsupported
            };
        }

        internal static SqlDataType ParseSqlDataType(this string typeString) => typeString.ToUpperInvariant() switch
        {
            "SMALLINT" => SqlDataType.SMALLINT,
            "INT" => SqlDataType.INT,
            "BIGINT" => SqlDataType.BIGINT,
            "SINGLE" => SqlDataType.SINGLE,
            "DOUBLE" => SqlDataType.DOUBLE,
            "DECIMAL" => SqlDataType.DECIMAL,
            "TINYINT" => SqlDataType.TINYINT,
            "TEXT" => SqlDataType.TEXT,
            "BOOLEAN" => SqlDataType.BOOLEAN,
            "DATETIME" => SqlDataType.DATETIME,
            _ => SqlDataType.Unsupported
        };

        internal static SqlDataType ToSqlDataType(this CLRDataType clrDataType) => clrDataType switch
        {
            CLRDataType.Int16 => SqlDataType.SMALLINT,
            CLRDataType.Int32 => SqlDataType.INT,
            CLRDataType.Int64 => SqlDataType.BIGINT,
            CLRDataType.Single => SqlDataType.SINGLE,
            CLRDataType.Double => SqlDataType.DOUBLE,
            CLRDataType.Decimal => SqlDataType.DECIMAL,
            CLRDataType.Byte => SqlDataType.TINYINT,
            CLRDataType.String => SqlDataType.TEXT,
            CLRDataType.Boolean => SqlDataType.BOOLEAN,
            CLRDataType.DateTime => SqlDataType.DATETIME,
            _ => SqlDataType.Unsupported
        };

        internal static DbType? ToDbTypeCode(this SqlDataType sqlDataType) => sqlDataType switch
        {
            SqlDataType.SMALLINT => DbType.Int16,
            SqlDataType.INT => DbType.Int32,
            SqlDataType.BIGINT => DbType.Int64,
            SqlDataType.SINGLE => DbType.Single,
            SqlDataType.DOUBLE => DbType.Double,
            SqlDataType.DECIMAL => DbType.Decimal,
            SqlDataType.TINYINT => DbType.Byte,
            SqlDataType.TEXT => DbType.String,
            SqlDataType.BOOLEAN => DbType.Boolean,
            SqlDataType.DATETIME => DbType.DateTime,
            _ => null
        };


        internal static CLRDataType ToClrTypeCode(this SqlDataType sqlDataType) => sqlDataType switch
        {
            SqlDataType.SMALLINT => CLRDataType.Int16,
            SqlDataType.INT => CLRDataType.Int32,
            SqlDataType.BIGINT => CLRDataType.Int64,
            SqlDataType.SINGLE => CLRDataType.Single,
            SqlDataType.DOUBLE => CLRDataType.Double,
            SqlDataType.DECIMAL => CLRDataType.Decimal,
            SqlDataType.TINYINT => CLRDataType.Byte,
            SqlDataType.TEXT => CLRDataType.String,
            SqlDataType.BOOLEAN => CLRDataType.Boolean,
            SqlDataType.DATETIME => CLRDataType.DateTime,
            _ => CLRDataType.Unsupported
        };

        private static string MakeSqlValueLiteral(this object? o)
        {
            switch (o)
            {
                case null: return "NULL";
                case true: return "1";
                case false: return "0";
                case char _:
                case string _: return $"\'{o}\'";
                case DateTime _: return $"DATETIME(\'{o:O}\')";
                default: return $"{o}";
            }
        }

        private static string GetSqlOperator(Operators op) => op switch
        {
            EQUALS => "=",
            NOT_EQUALS => "<>",
            LESS_THAN => "<",
            GREATER_THAN => ">",
            LESS_THAN_OR_EQUALS => "<=",
            GREATER_THAN_OR_EQUALS => ">=",
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };

        internal static string? ToSqliteWhereClause<T>(this IEnumerable<Condition<T>> conditions) where T : class
        {
            var values = string.Join(" AND ", conditions.Where(c => !c.Skip).Select(c =>
            {
                var op = GetSqlOperator(c.Operator);
                var key = c.Term.First!.ActualName;
                var valueLiteral = MakeSqlValueLiteral(c.Value);
                if (valueLiteral == "NULL")
                {
                    op = c.Operator switch
                    {
                        EQUALS => "IS",
                        NOT_EQUALS => "IS NOT",
                        _ => throw new SqliteException($"Operator '{op}' is not valid for comparison with NULL")
                    };
                }
                return $"{key.Fnuttify()} {op} {valueLiteral}";
            }));
            return string.IsNullOrWhiteSpace(values) ? null : "WHERE " + values;
        }
    }
}