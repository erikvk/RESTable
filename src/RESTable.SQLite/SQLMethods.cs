using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using RESTable.Requests;
using static RESTable.Requests.Operators;

namespace RESTable.Sqlite;

internal static class SqlMethods
{
    internal static string Fnuttify(this string sqlKey)
    {
        return $"\"{sqlKey}\"";
    }

    internal static bool IsSqliteCompatibleValueType(this Type type)
    {
        return type.ResolveClrTypeCode() != ClrDataType.Unsupported;
    }

    internal static ClrDataType ResolveClrTypeCode(this Type type)
    {
        if (type.IsNullable(out var baseType))
            type = baseType!;
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Int16 => ClrDataType.Int16,
            TypeCode.Int32 => ClrDataType.Int32,
            TypeCode.Int64 => ClrDataType.Int64,
            TypeCode.Single => ClrDataType.Single,
            TypeCode.Double => ClrDataType.Double,
            TypeCode.Decimal => ClrDataType.Decimal,
            TypeCode.Byte => ClrDataType.Byte,
            TypeCode.String => ClrDataType.String,
            TypeCode.Boolean => ClrDataType.Boolean,
            TypeCode.DateTime => ClrDataType.DateTime,
            _ => ClrDataType.Unsupported
        };
    }

    internal static SqlDataType ParseSqlDataType(this string typeString)
    {
        return typeString.ToUpperInvariant() switch
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
    }

    internal static SqlDataType ToSqlDataType(this ClrDataType clrDataType)
    {
        return clrDataType switch
        {
            ClrDataType.Int16 => SqlDataType.SMALLINT,
            ClrDataType.Int32 => SqlDataType.INT,
            ClrDataType.Int64 => SqlDataType.BIGINT,
            ClrDataType.Single => SqlDataType.SINGLE,
            ClrDataType.Double => SqlDataType.DOUBLE,
            ClrDataType.Decimal => SqlDataType.DECIMAL,
            ClrDataType.Byte => SqlDataType.TINYINT,
            ClrDataType.String => SqlDataType.TEXT,
            ClrDataType.Boolean => SqlDataType.BOOLEAN,
            ClrDataType.DateTime => SqlDataType.DATETIME,
            _ => SqlDataType.Unsupported
        };
    }

    internal static DbType? ToDbTypeCode(this SqlDataType sqlDataType)
    {
        return sqlDataType switch
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
    }


    internal static ClrDataType ToClrTypeCode(this SqlDataType sqlDataType)
    {
        return sqlDataType switch
        {
            SqlDataType.SMALLINT => ClrDataType.Int16,
            SqlDataType.INT => ClrDataType.Int32,
            SqlDataType.BIGINT => ClrDataType.Int64,
            SqlDataType.SINGLE => ClrDataType.Single,
            SqlDataType.DOUBLE => ClrDataType.Double,
            SqlDataType.DECIMAL => ClrDataType.Decimal,
            SqlDataType.TINYINT => ClrDataType.Byte,
            SqlDataType.TEXT => ClrDataType.String,
            SqlDataType.BOOLEAN => ClrDataType.Boolean,
            SqlDataType.DATETIME => ClrDataType.DateTime,
            _ => ClrDataType.Unsupported
        };
    }

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

    private static string GetSqlOperator(Operators op)
    {
        return op switch
        {
            EQUALS => "=",
            NOT_EQUALS => "<>",
            LESS_THAN => "<",
            GREATER_THAN => ">",
            LESS_THAN_OR_EQUALS => "<=",
            GREATER_THAN_OR_EQUALS => ">=",
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }

    internal static string? ToSqliteWhereClause<T>(this IEnumerable<Condition<T>> conditions) where T : class
    {
        var values = string.Join(" AND ", conditions.Where(c => !c.Skip).Select(c =>
        {
            var op = GetSqlOperator(c.Operator);
            var key = c.Term.First!.ActualName;
            var valueLiteral = MakeSqlValueLiteral(c.Value);
            if (valueLiteral == "NULL")
                op = c.Operator switch
                {
                    EQUALS => "IS",
                    NOT_EQUALS => "IS NOT",
                    _ => throw new SqliteException($"Operator '{op}' is not valid for comparison with NULL")
                };
            return $"{key.Fnuttify()} {op} {valueLiteral}";
        }));
        return string.IsNullOrWhiteSpace(values) ? null : "WHERE " + values;
    }
}