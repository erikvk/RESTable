using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using RESTar.Internal;

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

        private const string OpMatchChars = "<>=!";

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
                if (conditionString == "")
                    throw new SyntaxException(ErrorCodes.InvalidConditionSyntax, "Invalid condition syntax");
                conditionString = conditionString.ReplaceFirst("%3E=", ">=", out var replaced);
                if (!replaced) conditionString = conditionString.ReplaceFirst("%3C=", "<=", out replaced);
                if (!replaced) conditionString = conditionString.ReplaceFirst("%3E", ">", out replaced);
                if (!replaced) conditionString = conditionString.ReplaceFirst("%3C", "<", out replaced);
            }

            var operatorCharacters = new string(conditionString.Where(c => OpMatchChars.Contains(c)).ToArray());
            if (!Operator.TryParse(operatorCharacters, out var op))
                throw new OperatorException(conditionString);
            var keyValuePair = conditionString.Split(new[] {op.Common}, StringSplitOptions.None);
            Key = WebUtility.UrlDecode(keyValuePair[0]);
            Operator = op;
            ValueLiteral = WebUtility.UrlDecode(keyValuePair[1]);
        }
    }
}