using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using RESTar.Linq;
using RESTar.OData;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Serialization;
using static RESTar.Internal.ErrorCodes;
using static RESTar.OData.QueryOptions;
using static Newtonsoft.Json.Formatting;
using static RESTar.Serialization.Serializer;

namespace RESTar.Protocols
{
    internal class ODataProtocolProvider : IProtocolProvider
    {
        internal static bool HasODataHeader(Dictionary<string, string> headers)
        {
            return headers != null && (headers.TryGetNoCase("odata-version", out var v) ||
                                       headers.TryGetNoCase("odata-maxversion", out v)) && v == "4.0";
        }

        public string MakeRelativeUri(IUriParameters parameters)
        {
            var hasFilter = parameters.UriConditions.Count > 0;
            var hasOther = parameters.UriMetaConditions.Count > 0;

            using (var b = new StringWriter())
            {
                b.Write(parameters.ResourceSpecifier);
                if (hasFilter || hasOther)
                {
                    b.Write('?');
                    if (hasFilter)
                    {
                        b.Write("$filter=");
                        var conds = parameters.UriConditions.Select(c => $"{c.Key} {GetOperatorString(c.Operator.OpCode)} {c.ValueLiteral}");
                        b.Write(string.Join(" and ", conds));
                    }

                    if (hasOther)
                    {
                        if (hasFilter) b.Write("&");
                        var conds = parameters.UriMetaConditions.Select(c =>
                        {
                            switch (c.Key)
                            {
                                case "order_asc": return $"$orderby={c.ValueLiteral} asc";
                                case "order_desc": return $"$orderby={c.ValueLiteral} desc";
                                case "select": return $"$select={c.ValueLiteral}";
                                case "offset": return $"$skip={c.ValueLiteral}";
                                case "limit": return $"$top={c.ValueLiteral}";
                                default: throw new Exception();
                            }
                        });
                        b.Write(string.Join("&", conds));
                    }
                }

                return b.ToString();
            }
        }

        private static void PopulateFromUri(Arguments args, string uri)
        {
            var uriMatch = Regex.Match(uri, RegEx.ODataRequestUri);
            if (!uriMatch.Success) throw new SyntaxException(InvalidUriSyntax, "Check URI syntax");
            var entitySet = uriMatch.Groups["entityset"].Value.TrimStart('/');
            var options = uriMatch.Groups["options"].Value.TrimStart('?');
            if (entitySet.Length != 0)
                args.ResourceSpecifier = entitySet;
            if (options.Length != 0)
                PopulateFromOptions(args, options);
        }

        private static void PopulateFromOptions(IUriParameters args, string options)
        {
            foreach (var (optionKey, optionValue) in options.Split('&').Select(option => option.TSplit('=')))
            {
                if (string.IsNullOrWhiteSpace(optionKey))
                    throw new SyntaxException(InvalidConditionSyntax, "An OData query option key was invalid");
                if (string.IsNullOrWhiteSpace(optionValue))
                    throw new SyntaxException(InvalidConditionSyntax, $"The OData query option value for {optionKey} was invalid");
                var decodedValue = HttpUtility.UrlDecode(optionValue);
                switch (optionKey)
                {
                    case var system when optionKey[0] == '$':
                        if (!Enum.TryParse(system.Substring(1), out QueryOptions option) || option == none)
                            throw new FeatureNotImplementedException($"Unknown or not implemented query option '{system}'");
                        switch (option)
                        {
                            case filter:
                                if (Regex.Match(decodedValue, RegEx.UnsupportedODataOperatorRegex) is Match m && m.Success)
                                    throw new FeatureNotImplementedException($"Not implemented operator '{m.Value}' in $filter");
                                decodedValue.Replace("(", "").Replace(")", "").Split(" and ").Select(c =>
                                {
                                    var parts = c.Split(' ');
                                    if (parts.Length != 3)
                                        throw new SyntaxException(InvalidConditionSyntax, "Invalid syntax in $filter query option");
                                    return new UriCondition(parts[0], GetOperator(parts[1]), parts[2]);
                                }).ForEach(args.UriConditions.Add);

                                break;

                            case orderby:
                                if (decodedValue.Contains(","))
                                    throw new FeatureNotImplementedException("Multiple expressions not implemented for $orderby");
                                var (term, order) = decodedValue.TSplit(' ');
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
                                args.UriMetaConditions.Add(new UriCondition("select", Operators.EQUALS, decodedValue));
                                break;

                            case skip:
                                args.UriMetaConditions.Add(new UriCondition("offset", Operators.EQUALS, decodedValue));
                                break;

                            case top:
                                args.UriMetaConditions.Add(new UriCondition("limit", Operators.EQUALS, decodedValue));
                                break;

                            default: throw new ArgumentOutOfRangeException();
                        }

                        break;
                }
            }
        }

        private static string GetOperatorString(Operators op)
        {
            switch (op)
            {
                case Operators.EQUALS: return "eq";
                case Operators.NOT_EQUALS: return "ne";
                case Operators.LESS_THAN: return "lt";
                case Operators.GREATER_THAN: return "gt";
                case Operators.LESS_THAN_OR_EQUALS: return "le";
                case Operators.GREATER_THAN_OR_EQUALS: return "ge";
                default: throw new FeatureNotImplementedException($"Unknown or not implemented operator '{op}' in $filter");
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
                default: throw new FeatureNotImplementedException($"Unknown or not implemented operator '{op}' in $filter");
            }
        }

        public IFinalizedResult FinalizeResult(Result result)
        {
            if (result.Entities is IEnumerable<AvailableResource> availableResources)
                result.Entities = ServiceDocument.Make(availableResources);

            if (result.Entities != null)
            {
                var stream = new MemoryStream();
                using (var swr = new StreamWriter(stream, UTF8, 1024, true))
                using (var jwr = new ODataJsonWriter(swr))
                {
                    JsonSerializer.Formatting = Indented;
                    jwr.WritePre();
                    JsonSerializer.Serialize(jwr, result.Entities);
                    result.EntityCount = jwr.ObjectsWritten;
                    jwr.WriteRaw(",");
                    jwr.WriteIndentation();
                    jwr.WriteRaw($"\"@odata.count\": {result.EntityCount}");
                    if (result.IsPaged)
                    {
                        jwr.WriteRaw(",");
                        jwr.WriteIndentation();
                        jwr.WriteRaw($"\"@odata.nextLink\": {result.EntityCount}");
                    }
                    jwr.WritePost();
                }

                if (result.HasContent)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    result.Body = stream;
                }
            }

            return result;
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