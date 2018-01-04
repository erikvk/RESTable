using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using RESTar.Internal;
using RESTar.Results.Fail.BadRequest;

namespace RESTar.Requests
{
    /// <summary>
    /// Describes the syntactic components of a RESTar uri condition
    /// </summary>
    public struct UriCondition
    {
        internal string Key { get; }
        internal Operator Operator { get; }
        internal string ValueLiteral { get; set; }

        internal static IEnumerable<UriCondition> ParseMany(string conditionsString, bool check = false) =>
            conditionsString.Split('&').Select(s => new UriCondition(s, check));

        /// <inheritdoc />
        public override string ToString() => $"{Key}{Operator.Common}{ValueLiteral}";

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
    }
}