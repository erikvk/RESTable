using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using RESTar.Internal;
using RESTar.Results;
using static System.StringComparison;

namespace RESTar.Requests
{
    /// <inheritdoc cref="IUriCondition" />
    /// <summary>
    /// Describes the syntactic components of a RESTar uri condition
    /// </summary>
    public struct UriCondition : IUriCondition
    {
        /// <inheritdoc />
        public string Key { get; }

        /// <summary>
        /// The operator used for determining the comparison operation
        /// </summary>
        public Operators OperatorCode => Operator.OpCode;

        Operators IUriCondition.Operator => OperatorCode;

        internal Operator Operator { get; }

        /// <inheritdoc />
        public string ValueLiteral { get; }

        internal static List<UriCondition> ParseMany(string conditionsString, bool check = false) => conditionsString
            .Split('&')
            .Select(s => new UriCondition(s, check))
            .ToList();

        /// <summary>
        /// Creates a new custom UriCondition
        /// </summary>
        public UriCondition(string key, Operators op, string valueLiteral)
        {
            Key = key;
            Operator = op;
            ValueLiteral = valueLiteral;
        }

        /// <summary>
        /// Creates a new UriCondition from a RESTar condition string
        /// </summary>
        public UriCondition(string conditionString, bool check = false)
        {
            if (check)
            {
                if (string.IsNullOrEmpty(conditionString))
                    throw new InvalidSyntax(ErrorCodes.InvalidConditionSyntax, "Invalid condition syntax");
                conditionString = conditionString.ReplaceFirst("%3E=", ">=", out var replaced);
                if (!replaced) conditionString = conditionString.ReplaceFirst("%3C=", "<=", out replaced);
                if (!replaced) conditionString = conditionString.ReplaceFirst("%3E", ">", out replaced);
                if (!replaced) conditionString = conditionString.ReplaceFirst("%3C", "<", out replaced);
            }
            var match = Regex.Match(conditionString, RegEx.UriCondition);
            if (!match.Success)
                throw new InvalidSyntax(ErrorCodes.InvalidConditionSyntax, $"Invalid condition syntax at '{conditionString}'");
            var (key, opString, valueLiteral) = (match.Groups["key"].Value, match.Groups["op"].Value, match.Groups["val"].Value);
            Key = WebUtility.UrlDecode(key);
            if (!Operator.TryParse(opString, out var op))
                throw new InvalidOperator(conditionString);
            Operator = op;
            ValueLiteral = WebUtility.UrlDecode(valueLiteral);
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is UriCondition u &&
                                                   string.Equals(u.Key, Key, OrdinalIgnoreCase) &&
                                                   u.Operator == Operator &&
                                                   u.ValueLiteral == ValueLiteral;

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + StringComparer.OrdinalIgnoreCase.GetHashCode(Key);
                hash = hash * 23 + Operator.OpCode.GetHashCode();
                hash = hash * 23 + ValueLiteral.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// EqualityComparer for UriCondition objects
        /// </summary>
        public static readonly IEqualityComparer<UriCondition> EqualityComparer = new _EqualityComparer();

        private class _EqualityComparer : IEqualityComparer<UriCondition>
        {
            public bool Equals(UriCondition x, UriCondition y) => string.Equals(x.Key, y.Key, OrdinalIgnoreCase)
                                                                  && x.Operator == y.Operator
                                                                  && x.ValueLiteral == y.ValueLiteral;

            public int GetHashCode(UriCondition obj)
            {
                unchecked
                {
                    var hash = 17;
                    hash = hash * 23 + StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Key);
                    hash = hash * 23 + obj.Operator.OpCode.GetHashCode();
                    hash = hash * 23 + obj.ValueLiteral.GetHashCode();
                    return hash;
                }
            }
        }
    }
}