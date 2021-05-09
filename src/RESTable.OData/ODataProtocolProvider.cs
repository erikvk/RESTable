using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Meta;
using RESTable.ProtocolProviders;
using RESTable.Requests;
using RESTable.Results;
using static RESTable.ErrorCodes;
using static RESTable.OData.QueryOptions;
using static RESTable.Requests.RESTableMetaCondition;

namespace RESTable.OData
{
    /// <inheritdoc />
    /// <summary>
    /// The protocol provider for OData. Instantiate this class and include a reference in 
    /// the 'protocolProviders' parameter of RESTableConfig.Init().
    /// </summary>
    public class ODataProtocolProvider : IProtocolProvider
    {
        /// <inheritdoc />
        public IEnumerable<IContentTypeProvider> GetCustomContentTypeProviders()
        {
            yield return JsonProvider;
        }

        /// <inheritdoc />
        public ExternalContentTypeProviderSettings ExternalContentTypeProviderSettings => ExternalContentTypeProviderSettings.DontAllow;

        /// <inheritdoc />
        public string ProtocolName => "OData v4.0";

        /// <inheritdoc />
        public string ProtocolIdentifier => "OData";

        /// <inheritdoc />
        public string MakeRelativeUri(IUriComponents components)
        {
            var hasFilter = components.Conditions.Any();
            var hasOther = components.MetaConditions.Any();
            using (var b = new StringWriter())
            {
                b.Write('/');
                b.Write(components.ResourceSpecifier);
                if (hasFilter || hasOther)
                {
                    b.Write('?');
                    if (hasFilter)
                    {
                        b.Write("$filter=");
                        var conds = components.Conditions.Select(c => $"{c.Key} {GetOperatorString(c.Operator)} {c.ValueLiteral}");
                        b.Write(string.Join(" and ", conds));
                    }

                    if (hasOther)
                    {
                        if (hasFilter) b.Write("&");
                        var conds = components.MetaConditions.Select(c => c.Key switch
                        {
                            "order_asc" => $"$orderby={c.ValueLiteral} asc",
                            "order_desc" => $"$orderby={c.ValueLiteral} desc",
                            "select" => $"$select={c.ValueLiteral}",
                            "offset" => $"$skip={c.ValueLiteral}",
                            "limit" => $"$top={c.ValueLiteral}",
                            "search" => $"$search={c.ValueLiteral}",
                            _ => ""
                        });
                        b.Write(string.Join("&", conds));
                    }
                }
                return b.ToString();
            }
        }

        /// <inheritdoc />
        public void OnInit() { }

        /// <inheritdoc />
        public IUriComponents GetUriComponents(string uriString, RESTableContext context)
        {
            var uriMatch = Regex.Match(uriString, @"(?<entityset>/[^/\?]*)?(?<options>\?[^/]*)?");
            if (!uriMatch.Success) throw new InvalidODataSyntax(InvalidUriSyntax, "Check URI syntax");
            var entitySet = uriMatch.Groups["entityset"].Value.TrimStart('/');
            var options = uriMatch.Groups["options"].Value.TrimStart('?');
            var uri = new ODataUriComponents(this);
            var resources = context.Services.GetRequiredService<ResourceCollection>();
            uri.ResourceSpecifier = entitySet switch
            {
                "" => resources.GetResourceSpecifier<ServiceDocument>(),
                "$metadata" => resources.GetResourceSpecifier<MetadataDocument>(),
                _ => entitySet
            };
            if (options.Length != 0)
                PopulateFromOptions(uri, options);
            return uri;
        }

        private IJsonProvider JsonProvider { get; }
        private RESTableConfiguration Configuration { get; }

        public ODataProtocolProvider(IJsonProvider jsonProvider, RESTableConfiguration configuration)
        {
            JsonProvider = jsonProvider;
            Configuration = configuration;
        }

        private static void PopulateFromOptions(ODataUriComponents args, string options)
        {
            foreach (var (optionKey, optionValue) in options.Split('&').Select(option => option.TupleSplit('=')))
            {
                if (string.IsNullOrWhiteSpace(optionKey))
                    throw new InvalidODataSyntax(InvalidConditionSyntax, "An OData query option key was null or whitespace");
                if (string.IsNullOrWhiteSpace(optionValue))
                    throw new InvalidODataSyntax(InvalidConditionSyntax, $"The OData query option value for '{optionKey}' was invalid");
                var decodedValue = HttpUtility.UrlDecode(optionValue);
                switch (optionKey)
                {
                    case var system when optionKey[0] == '$':
                        if (!Enum.TryParse(system.Substring(1), out QueryOptions option) || option == none)
                            throw new FeatureNotImplemented($"Unknown or not implemented query option '{system}'");
                        switch (option)
                        {
                            case filter:
                                if (Regex.Match(decodedValue, @"(/| has | not | cast\(.*\)| mul | div | mod | add | sub | isof | or )") is Match {Success: true} m)
                                    throw new FeatureNotImplemented($"Not implemented operator '{m.Value}' in $filter");
                                var toAdd = decodedValue
                                    .Replace("(", "")
                                    .Replace(")", "")
                                    .Split(" and ")
                                    .Select<string, IUriCondition>(c =>
                                    {
                                        var parts = c.Split(' ');
                                        if (parts.Length != 3)
                                            throw new InvalidODataSyntax(InvalidConditionSyntax, "Invalid syntax in $filter query option");
                                        return new UriCondition(parts[0], GetOperator(parts[1]), parts[2], TypeCode.String);
                                    });
                                args.Conditions.AddRange(toAdd);
                                break;
                            case orderby:
                                if (decodedValue.Contains(","))
                                    throw new FeatureNotImplemented("Multiple expressions not implemented for $orderby");
                                var (term, order) = decodedValue.TupleSplit(' ');
                                switch (order)
                                {
                                    case null:
                                    case "":
                                    case "asc":
                                        args.MetaConditions.Add(new UriCondition(Order_asc, term));
                                        break;
                                    case "desc":
                                        args.MetaConditions.Add(new UriCondition(Order_desc, term));
                                        break;
                                    default:
                                        throw new InvalidODataSyntax(InvalidConditionSyntax,
                                            "The OData query option value for $orderby was invalid");
                                }
                                break;
                            case select:
                                args.MetaConditions.Add(new UriCondition(Select, decodedValue));
                                break;
                            case skip:
                                args.MetaConditions.Add(new UriCondition(Offset, decodedValue));
                                break;
                            case top:
                                args.MetaConditions.Add(new UriCondition(Limit, decodedValue));
                                break;
                            case search:
                                args.MetaConditions.Add(new UriCondition(Search, decodedValue));
                                break;
                            default: throw new ArgumentOutOfRangeException();
                        }
                        break;
                    default:
                        args.MetaConditions.Add(new UriCondition(optionKey, Operators.EQUALS, optionValue, TypeCode.String));
                        break;
                }
            }
        }

        private static string GetOperatorString(Operators op) => op switch
        {
            Operators.EQUALS => "eq",
            Operators.NOT_EQUALS => "ne",
            Operators.LESS_THAN => "lt",
            Operators.GREATER_THAN => "gt",
            Operators.LESS_THAN_OR_EQUALS => "le",
            Operators.GREATER_THAN_OR_EQUALS => "ge",
            _ => throw new FeatureNotImplemented($"Unknown or not implemented operator '{op}' in $filter")
        };

        private static Operators GetOperator(string op) => op switch
        {
            "eq" => Operators.EQUALS,
            "ne" => Operators.NOT_EQUALS,
            "lt" => Operators.LESS_THAN,
            "gt" => Operators.GREATER_THAN,
            "le" => Operators.LESS_THAN_OR_EQUALS,
            "ge" => Operators.GREATER_THAN_OR_EQUALS,
            _ => throw new FeatureNotImplemented($"Unknown or not implemented operator '{op}' in $filter")
        };

        /// <inheritdoc />
        public bool IsCompliant(IRequest request, out string invalidReason)
        {
            invalidReason = null;
            switch (request.Headers["OData-Version"] ?? request.Headers["OData-MaxVersion"])
            {
                case null:
                case "4.0": return true;
                default:
                    invalidReason = "Unsupported OData protocol version. Supported protocol version: 4.0";
                    return false;
            }
        }

        private string GetServiceRoot(IEntities entities)
        {
            if (entities is null) throw new ArgumentNullException(nameof(entities));
            var origin = entities.Request.Context.Client;
            var hostAndPath = $"{origin.Host}{Configuration.RootUri}-odata";
            return origin.Https ? $"https://{hostAndPath}" : $"http://{hostAndPath}";
        }

        public void SetResultHeaders(IResult result)
        {
            result.Headers.ContentType = "application/json; odata.metadata=minimal; odata.streaming=true; charset=utf-8";
            result.Headers["OData-Version"] = "4.0";
        }

        /// <inheritdoc />
        public async Task SerializeResult(ISerializedResult toSerialize, IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken)
        {
            switch (toSerialize.Result)
            {
                case Binary binary:
                    await binary.BinaryResult.WriteToStream(toSerialize.Body, cancellationToken).ConfigureAwait(false);
                    return;
                case not IEntities: return;
            }

            string contextFragment;
            bool writeMetadata;
            var entities = (IEntities) toSerialize.Result;
            switch (entities)
            {
                case IEntities<ServiceDocument> _:
                    contextFragment = null;
                    writeMetadata = false;
                    break;
                default:
                    contextFragment = $"#{entities.Request.Resource.Name}";
                    writeMetadata = true;
                    break;
            }
            var swr = new StreamWriter(toSerialize.Body, Encoding.Default, 4096, true);
#if NETSTANDARD2_1
            await using (swr)
#else
            using (swr)
#endif
            {
                using var jwr = JsonProvider.GetJsonWriter(swr);
                await jwr.WriteStartObjectAsync(cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("@odata.context", cancellationToken).ConfigureAwait(false);
                await jwr.WriteValueAsync($"{GetServiceRoot(entities)}/$metadata{contextFragment}", cancellationToken).ConfigureAwait(false);
                await jwr.WritePropertyNameAsync("value", cancellationToken).ConfigureAwait(false);
                jwr.StartCountObjectsWritten();
                JsonProvider.Serialize(jwr, entities);
                toSerialize.EntityCount = jwr.StopCountObjectsWritten();
                if (writeMetadata)
                {
                    await jwr.WritePropertyNameAsync("@odata.count", cancellationToken).ConfigureAwait(false);
                    await jwr.WriteValueAsync(toSerialize.EntityCount, cancellationToken).ConfigureAwait(false);
                    if (toSerialize.HasNextPage)
                    {
                        await jwr.WritePropertyNameAsync("@odata.nextLink", cancellationToken).ConfigureAwait(false);
                        await jwr.WriteValueAsync(MakeRelativeUri(entities.GetNextPageLink(toSerialize.EntityCount, -1)), cancellationToken).ConfigureAwait(false);
                    }
                }
                await jwr.WriteEndObjectAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}