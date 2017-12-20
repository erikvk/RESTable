using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Requests;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Protocols.ODataProtocolProvider.QueryOptions;

namespace RESTar.Protocols
{
    internal class ODataProtocolProvider : IProtocolProvider
    {
        internal enum QueryOptions
        {
            none,
            filter,
            orderby,
            select,
            skip,
            top
        }

        internal static bool HasODataHeader(Dictionary<string, string> headers)
        {
            return headers != null && (headers.TryGetNoCase("odata-version", out var v) ||
                                       headers.TryGetNoCase("odata-maxversion", out v)) && v == "4.0";
        }

        private static void PopulateFromUri(Arguments args, string uri)
        {
            var uriMatch = Regex.Match(uri, RegEx.ODataRequestUri);
            if (!uriMatch.Success) throw new SyntaxException(InvalidUriSyntax, "Check URI syntax");
            var entitySet = uriMatch.Groups["entityset"].Value.TrimStart('/');
            var options = uriMatch.Groups["options"].Value.TrimStart('?');
            args.ResourceSpecifier = entitySet;
            foreach (var (oKey, oValue) in options.Split('&').Select(option => option.TSplit('=')))
            {
                if (string.IsNullOrWhiteSpace(oKey))
                    throw new SyntaxException(InvalidConditionSyntax, "An OData query option key was invalid");
                if (string.IsNullOrWhiteSpace(oValue))
                    throw new SyntaxException(InvalidConditionSyntax, $"The OData query option value for {oKey} was invalid");
                switch (oKey)
                {
                    case var system when oKey[0] == '$':
                        if (!Enum.TryParse(system.Substring(1), out QueryOptions option) || option == none)
                            throw new FeatureNotImplementedException($"Unknown or not implemented query option '{system}'");
                        switch (option)
                        {
                            case filter:
                                if (Regex.Match(oValue, RegEx.UnsupportedODataOperatorRegex) is Match m && m.Success)
                                    throw new FeatureNotImplementedException($"Not implemented operator '{m.Value}' in $filter");
                                oValue.Replace("(", "").Replace(")", "").Split(" and ").Select(c =>
                                {
                                    var parts = c.Split(' ');
                                    if (parts.Length != 3)
                                        throw new SyntaxException(InvalidConditionSyntax, "Invalid syntax in $filter query option");
                                    return new UriCondition(parts[0], GetOperator(parts[1]), parts[2]);
                                }).ForEach(args.UriConditions.Add);

                                break;

                            case orderby:
                                if (oValue.Contains(","))
                                    throw new FeatureNotImplementedException("Multiple expressions not implemented for $orderby");
                                var (term, order) = oValue.TSplit(' ');
                                switch (order)
                                {
                                    case null:
                                    case "":
                                    case "asc":
                                        args.UriMetaConditions.Add(new UriCondition("order_asc", Operators.EQUALS, term));
                                        break;
                                    case "desc":
                                        args.UriMetaConditions.Add(new UriCondition("order_desc", Operators.EQUALS, term));
                                        break;
                                    default:
                                        throw new SyntaxException(InvalidConditionSyntax,
                                            "The OData query option value for $orderby was invalid");
                                }

                                break;

                            case select:
                                args.UriMetaConditions.Add(new UriCondition("select", Operators.EQUALS, oValue));
                                break;

                            case skip:
                                args.UriMetaConditions.Add(new UriCondition("offset", Operators.EQUALS, oValue));
                                break;

                            case top:
                                args.UriMetaConditions.Add(new UriCondition("limit", Operators.EQUALS, oValue));
                                break;

                            default: throw new ArgumentOutOfRangeException();
                        }

                        break;
                }
            }
        }

        private static Operators GetOperator(string op)
        {
            switch (op)
            {
                case "eq": return Operators.EQUALS;
                case "ne": return Operators.NOT_EQUALS;
                case "lt": return Operators.LESS_THAN;
                case "gt": return Operators.GREATER_THAN;
                case "le": return Operators.LESS_THAN_OR_EQUALS;
                case "ge": return Operators.GREATER_THAN_OR_EQUALS;
                default: throw new FeatureNotImplementedException($"Not implemented operator ' {op} ' in $filter");
            }
        }

        public IFinalizedResult FinalizeResult(Result result)
        {
            return null;
        }

        internal enum MetaDataLevel
        {
            None,
            Minimal,
            All
        }

        public Arguments MakeRequestArguments(string uri, byte[] body, IDictionary<string, string> headers,
            string contentType, string accept)
        {
            var args = new Arguments
            {
                BodyBytes = body,
                Headers = headers,
                ContentType = contentType,
                Accept = accept
            };
            PopulateFromUri(args, uri);
            return args;
        }
    }
}