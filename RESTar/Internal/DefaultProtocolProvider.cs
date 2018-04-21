using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using RESTar.Admin;
using RESTar.Requests;
using RESTar.Results;
using RESTar.WebSockets;

namespace RESTar.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// Contains the logic for the default RESTar protocol. This protocol is used if no 
    /// protocol indicator is included in the request URI.
    /// </summary>
    internal sealed class DefaultProtocolProvider : IProtocolProvider
    {
        /// <inheritdoc />
        public string ProtocolName { get; } = "RESTar";

        /// <inheritdoc />
        public string ProtocolIdentifier { get; } = "restar";

        /// <inheritdoc />
        public void PopulateURI(string uriString, URI uri, Context context)
        {
            var match = Regex.Match(uriString, RegEx.RESTarRequestUri);
            if (!match.Success) throw new InvalidSyntax(ErrorCodes.InvalidUriSyntax, "Check URI syntax");
            var resourceOrMacro = match.Groups["res"].Value.TrimStart('/');
            var view = match.Groups["view"].Value.TrimStart('-');
            var conditions = match.Groups["cond"].Value.TrimStart('/');
            var metaConditions = match.Groups["meta"].Value.TrimStart('/');

            switch (conditions)
            {
                case var _ when conditions.Length == 0:
                case var _ when conditions == "_": break;
                default:
                    uri.Conditions.AddRange(UriCondition.ParseMany(conditions, true));
                    break;
            }

            switch (metaConditions)
            {
                case var _ when metaConditions.Length == 0:
                case var _ when metaConditions == "_": break;
                default:
                    uri.MetaConditions.AddRange(UriCondition.ParseMany(metaConditions, true));
                    break;
            }

            if (view.Length != 0)
                uri.ViewName = view;

            switch (resourceOrMacro)
            {
                case "":
                case "_":
                    uri.ResourceSpecifier = context.WebSocket?.Status == WebSocketStatus.Waiting
                        ? Shell.TerminalResource.Name
                        : EntityResource<AvailableResource>.ResourceSpecifier;
                    break;
                case var resource when resourceOrMacro[0] != '$':
                    uri.ResourceSpecifier = resource;
                    break;
                case var macro:
                    var macroName = macro.Substring(1);
                    uri.Macro = DbMacro.Get(macroName) ?? throw new UnknownMacro(macroName);
                    uri.ResourceSpecifier = uri.Macro.ResourceSpecifier;
                    uri.ViewName = uri.ViewName ?? uri.Macro.ViewName;
                    var macroConditions = uri.Macro.UriConditions;
                    if (macroConditions != null)
                        uri.Conditions.AddRange(macroConditions);
                    var macroMetaConditions = uri.Macro.UriMetaConditions;
                    if (macroMetaConditions != null)
                        uri.MetaConditions.AddRange(macroMetaConditions);
                    break;
            }
        }

        public ExternalContentTypeProviderSettings ExternalContentTypeProviderSettings { get; } = ExternalContentTypeProviderSettings.AllowAll;

        public IEnumerable<IContentTypeProvider> GetContentTypeProviders() => null;

        /// <inheritdoc />
        public string MakeRelativeUri(IUriComponents components) => ToUriString(components);

        private static string ToUriString(IUriComponents components)
        {
            var view = components.ViewName != null ? $"-{components.ViewName}" : null;
            return $"/{components.ResourceSpecifier}{view}/{ToUriString(components.Conditions)}/{ToUriString(components.MetaConditions)}";
        }

        private static string ToUriString(IEnumerable<IUriCondition> conditions)
        {
            if (conditions == null) return "_";
            var uriString = string.Join("&", conditions.Select(ToUriString));
            if (uriString.Length == 0) return "_";
            return uriString;
        }

        private static string ToUriString(IUriCondition condition)
        {
            if (condition == null) return "";
            return $"{WebUtility.UrlEncode(condition.Key)}{((Operator) condition.Operator).Common}{WebUtility.UrlEncode(condition.ValueLiteral)}";
        }

        /// <inheritdoc />
        public ISerializedResult Serialize(IResult result, IContentTypeProvider contentTypeProvider)
        {
            switch (result)
            {
                case Report report:
                    contentTypeProvider.SerializeEntity(report.ReportBody, report.Body, report.Request, out _);
                    return report;

                case Head head:
                    head.Headers.EntityCount = head.EntityCount.ToString();
                    return head;

                case IEntities<object> entities:

                    ISerializedResult SerializeEntities()
                    {
                        contentTypeProvider.SerializeCollection(entities, entities.Body, entities.Request, out var entityCount);
                        if (entityCount == 0) return new NoContent(entities.Request);
                        entities.Body.Seek(0, SeekOrigin.Begin);
                        entities.Headers.EntityCount = entityCount.ToString();
                        entities.EntityCount = entityCount;
                        if (entities.IsPaged)
                        {
                            var pager = entities.GetNextPageLink();
                            entities.Headers.Pager = MakeRelativeUri(pager);
                        }
                        entities.SetContentDisposition(contentTypeProvider.ContentDispositionFileExtension);
                        return entities;
                    }

                    if (entities.Request.Headers.Destination == null)
                        return SerializeEntities();
                    try
                    {
                        var parameters = new HeaderRequestParameters(entities.Request.Headers.Destination);
                        if (parameters.IsInternal)
                        {
                            var internalRequest = entities.Context.CreateRequest(parameters.Method, parameters.URI, null, parameters.Headers);
                            var serializedEntities = SerializeEntities();
                            if (!(serializedEntities is Content content))
                                return serializedEntities;
                            internalRequest.SetBody(content.Body);
                            return internalRequest.Result.Serialize();
                        }
                        var serialized = SerializeEntities();
                        var externalRequest = new HttpRequest(serialized, parameters, serialized.Body);
                        var response = externalRequest.GetResponse() ?? throw new InvalidExternalDestination(externalRequest, "No response");
                        if (response.StatusCode >= HttpStatusCode.BadRequest)
                            throw new InvalidExternalDestination(externalRequest,
                                $"Received {response.StatusCode.ToCode()} - {response.StatusDescription}. {response.Headers.Info}");
                        if (serialized.Headers.FirstOrDefault(pair => pair.Key.EqualsNoCase("Access-Control-Allow-Origin")).Value is string h)
                            response.Headers["Access-Control-Allow-Origin"] = h;
                        return new ExternalDestinationResult(entities.Request, response);
                    }
                    catch (HttpRequestException re)
                    {
                        throw new InvalidSyntax(ErrorCodes.InvalidDestination, $"{re.Message} in the Destination header");
                    }

                default: return result as ISerializedResult;
            }
        }

        private static void SetSelector<TRequest, TEntity>(IRequest<TRequest> r, IEntities<TEntity> e)
            where TRequest : class where TEntity : class, TRequest => r.Selector = () => e;

        public bool IsCompliant(IRequest request, out string invalidReason)
        {
            invalidReason = null;
            return true;
        }
    }
}