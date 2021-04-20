using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using RESTable.Requests;
using static RESTable.Requests.Operators;

namespace RESTable.SQLite
{
    internal static class SQLMethods
    {
        internal static string Fnuttify(this string sqlKey) => $"\"{sqlKey}\"";

        internal static bool IsSQLiteCompatibleValueType(this Type type) => type.ResolveCLRTypeCode() != CLRDataType.Unsupported;

        internal static CLRDataType ResolveCLRTypeCode(this Type type)
        {
            if (type.IsNullable(out var baseType))
                type = baseType;
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

        internal static SQLDataType ParseSQLDataType(this string typeString) => typeString.ToUpperInvariant() switch
        {
            "SMALLINT" => SQLDataType.SMALLINT,
            "INT" => SQLDataType.INT,
            "BIGINT" => SQLDataType.BIGINT,
            "SINGLE" => SQLDataType.SINGLE,
            "DOUBLE" => SQLDataType.DOUBLE,
            "DECIMAL" => SQLDataType.DECIMAL,
            "TINYINT" => SQLDataType.TINYINT,
            "TEXT" => SQLDataType.TEXT,
            "BOOLEAN" => SQLDataType.BOOLEAN,
            "DATETIME" => SQLDataType.DATETIME,
            _ => SQLDataType.Unsupported
        };

        internal static SQLDataType ToSQLDataType(this CLRDataType clrDataType) => clrDataType switch
        {
            CLRDataType.Int16 => SQLDataType.SMALLINT,
            CLRDataType.Int32 => SQLDataType.INT,
            CLRDataType.Int64 => SQLDataType.BIGINT,
            CLRDataType.Single => SQLDataType.SINGLE,
            CLRDataType.Double => SQLDataType.DOUBLE,
            CLRDataType.Decimal => SQLDataType.DECIMAL,
            CLRDataType.Byte => SQLDataType.TINYINT,
            CLRDataType.String => SQLDataType.TEXT,
            CLRDataType.Boolean => SQLDataType.BOOLEAN,
            CLRDataType.DateTime => SQLDataType.DATETIME,
            _ => SQLDataType.Unsupported
        };

        internal static DbType? ToDbTypeCode(this SQLDataType sqlDataType) => sqlDataType switch
        {
            SQLDataType.SMALLINT => DbType.Int16,
            SQLDataType.INT => DbType.Int32,
            SQLDataType.BIGINT => DbType.Int64,
            SQLDataType.SINGLE => DbType.Single,
            SQLDataType.DOUBLE => DbType.Double,
            SQLDataType.DECIMAL => DbType.Decimal,
            SQLDataType.TINYINT => DbType.Byte,
            SQLDataType.TEXT => DbType.String,
            SQLDataType.BOOLEAN => DbType.Boolean,
            SQLDataType.DATETIME => DbType.DateTime,
            _ => null
        };


        internal static CLRDataType ToCLRTypeCode(this SQLDataType sqlDataType) => sqlDataType switch
        {
            SQLDataType.SMALLINT => CLRDataType.Int16,
            SQLDataType.INT => CLRDataType.Int32,
            SQLDataType.BIGINT => CLRDataType.Int64,
            SQLDataType.SINGLE => CLRDataType.Single,
            SQLDataType.DOUBLE => CLRDataType.Double,
            SQLDataType.DECIMAL => CLRDataType.Decimal,
            SQLDataType.TINYINT => CLRDataType.Byte,
            SQLDataType.TEXT => CLRDataType.String,
            SQLDataType.BOOLEAN => CLRDataType.Boolean,
            SQLDataType.DATETIME => CLRDataType.DateTime,
            _ => CLRDataType.Unsupported
        };

        private static string MakeSQLValueLiteral(this object o)
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

        private static string GetSQLOperator(Operators op) => op switch
        {
            EQUALS => "=",
            NOT_EQUALS => "<>",
            LESS_THAN => "<",
            GREATER_THAN => ">",
            LESS_THAN_OR_EQUALS => "<=",
            GREATER_THAN_OR_EQUALS => ">=",
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };

        internal static string ToSQLiteWhereClause<T>(this IEnumerable<Condition<T>> conditions) where T : class
        {
            var values = string.Join(" AND ", conditions.Where(c => !c.Skip).Select(c =>
            {
                var op = GetSQLOperator(c.Operator);
                var key = c.Term.First.ActualName;
                var valueLiteral = MakeSQLValueLiteral(c.Value);
                if (valueLiteral == "NULL")
                {
                    op = c.Operator switch
                    {
                        EQUALS => "IS",
                        NOT_EQUALS => "IS NOT",
                        _ => throw new SQLiteException($"Operator '{op}' is not valid for comparison with NULL")
                    };
                }
                return $"{key.Fnuttify()} {op} {valueLiteral}";
            }));
            return string.IsNullOrWhiteSpace(values) ? null : "WHERE " + values;
        }
    }
}