﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTable.Meta;
using RESTable.Requests;
using Starcounter.Database;
using static RESTable.Requests.Operators;

namespace RESTable.Starcounter3x
{
    public static class ExtensionMethods
    {
        public static bool IsStarcounterDatabaseType(this MemberInfo type)
        {
            return type.HasAttribute<DatabaseAttribute>();
        }

        public static bool IsStarcounterCompatible(this Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => true,
                TypeCode.Char => true,
                TypeCode.SByte => true,
                TypeCode.Byte => true,
                TypeCode.Int16 => true,
                TypeCode.UInt16 => true,
                TypeCode.Int32 => true,
                TypeCode.UInt32 => true,
                TypeCode.Int64 => true,
                TypeCode.UInt64 => true,
                TypeCode.Single => true,
                TypeCode.Double => true,
                TypeCode.Decimal => true,
                TypeCode.DateTime => true,
                TypeCode.String => true,
                TypeCode.Empty => false,
                TypeCode.DBNull => false,
                TypeCode.Object when type.IsNullable(out var t) => IsStarcounterCompatible(t),
                TypeCode.Object when type == typeof(byte[]) => true,
                TypeCode.Object => type.IsStarcounterDatabaseType(),
                _ => false
            };
        }

        internal static (string WhereString, object[] Values) MakeWhereClause<T>
        (
            this IEnumerable<Condition<T>> conds,
            string orderByIndexName,
            out Dictionary<int, int> valuesAssignments,
            out bool useOrderBy
        ) where T : class
        {
            var _valuesAssignments = new Dictionary<int, int>();
            var literals = new List<object>();
            var hasOtherIndex = true;
            var clause = string.Join(" AND ", conds.Where(c => !c.Skip).Select((c, index) =>
            {
                var (key, op, value) = (c.Term.ActualNamesKey.Fnuttify(), c.InternalOperator.SQL, c.Value);
                if (value == null)
                {
                    switch (c.Operator)
                    {
                        case EQUALS:
                            op = "IS NULL";
                            break;
                        case NOT_EQUALS:
                            op = "IS NOT NULL";
                            break;
                        default: throw new Exception($"Operator '{op}' is not valid for comparison with NULL");
                    }
                    return $"t.{key} {op}";
                }
                literals.Add(c.Value);
                hasOtherIndex = false;
                _valuesAssignments[index] = literals.Count - 1;
                return $"t.{key} {c.InternalOperator.SQL} ? ";
            }));
            useOrderBy = !hasOtherIndex;
            if (clause.Length == 0)
            {
                valuesAssignments = null;
                return (null, null);
            }
            valuesAssignments = _valuesAssignments;
            return ($"WHERE {clause}", literals.ToArray());
        }

        internal static (string WhereString, object[] Values) MakeWhereClause<T>(this IEnumerable<Condition<T>> conds, string orderByIndexName,
            out bool useOrderBy) where T : class
        {
            var literals = new List<object>();
            var hasOtherIndex = false;
            var clause = string.Join(" AND ", conds.Where(c => !c.Skip).Select(c =>
            {
                var (key, op, value) = (c.Term.ActualNamesKey.Fnuttify(), c.InternalOperator.SQL, c.Value);
                if (value == null)
                {
                    switch (c.Operator)
                    {
                        case EQUALS:
                            op = "IS NULL";
                            break;
                        case NOT_EQUALS:
                            op = "IS NOT NULL";
                            break;
                        default: throw new Exception($"Operator '{op}' is not valid for comparison with NULL");
                    }
                    return $"t.{key} {op}";
                }
                literals.Add(c.Value);
                hasOtherIndex = false;
                return $"t.{key} {c.InternalOperator.SQL} ? ";
            }));
            useOrderBy = !hasOtherIndex;
            return clause.Length > 0 ? ($"WHERE {clause} ", literals.ToArray()) : (null, null);
        }

        public static bool IsStarcounterQueryable(this DeclaredProperty declaredProperty)
        {
            return declaredProperty.Owner.IsStarcounterDatabaseType() && declaredProperty.Type.IsStarcounterCompatible();
        }
        
        private static bool IsStarcounterQueryable<T>(this Condition<T> condition) where T : class
        {
            return condition.Term.HasFlag(Constants.StarcounterQueryableFlag);
        }

        internal static bool HasSQL<T>(this IEnumerable<Condition<T>> conds, out IEnumerable<Condition<T>> sql)
            where T : class
        {
            sql = conds.Where(IsStarcounterQueryable).ToList();
            return sql.Any();
        }

        public static IEnumerable<Condition<T>> GetSQL<T>(this IEnumerable<Condition<T>> conds) where T : class
        {
            return conds.Where(IsStarcounterQueryable);
        }

        internal static bool HasPost<T>(this IEnumerable<Condition<T>> conds, out List<Condition<T>> post)
            where T : class
        {
            post = conds.Where(c => !c.IsStarcounterQueryable() || c.IsOfType<string>() && c.Value != null).ToList();
            return post.Count > 0;
        }
    }
}