using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using RESTar.Admin;
using RESTar.Requests;
using RESTar.Results.Error;
using RESTar.Results.Error.BadRequest;
using RESTar.Results.Error.NotFound;
using RESTar.Results.Success;
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
        public ISerializedResult Serialize(Content content, IContentTypeProvider contentTypeProvider)
        {
            switch (content)
            {
                case Report report:
                    report.Headers["RESTar-count"] = report.ReportBody.Count.ToString();
                    contentTypeProvider.SerializeEntity(report.ReportBody, report.Body, report.Request, out var _);
                    return report;

                case IEntities<object> entities:
                    try
                    {
                        contentTypeProvider.SerializeCollection(entities, entities.Body, entities.Request, out var entityCount);
                        if (entityCount == 0)
                        {
                            entities.Body.Dispose();
                            return new NoContent(content, entities.Request.TimeElapsed);
                        }
                        entities.Headers["RESTar-count"] = entityCount.ToString();
                        entities.EntityCount = entityCount;
                        if (entities.IsPaged)
                        {
                            var pager = entities.GetNextPageLink();
                            entities.Headers["RESTar-pager"] = MakeRelativeUri(pager);
                        }
                        entities.SetContentDisposition(contentTypeProvider.ContentDispositionFileExtension);
                        if (entities.Request.Headers.Destination == null) return entities;
                        try
                        {
                            var request = new HttpRequest(entities, entities.Request.Headers.Destination)
                            {
                                ContentType = entities.ContentType.ToString(),
                                Body = entities.Body
                            };
                            var response = request.GetResponse() ?? throw new InvalidExternalDestination(request, "No response");
                            if (response.StatusCode >= HttpStatusCode.BadRequest)
                                throw new InvalidExternalDestination(request,
                                    $"Received {response.StatusCode.ToCode()} - {response.StatusDescription}. {response.Headers.SafeGet("RESTar-info")}");
                            if (entities.Headers.FirstOrDefault(pair => pair.Key.EqualsNoCase("Access-Control-Allow-Origin")).Value is string h)
                                response.Headers["Access-Control-Allow-Origin"] = h;
                            return response;
                        }
                        catch (HttpRequestException re)
                        {
                            throw new InvalidSyntax(ErrorCodes.InvalidDestination, $"{re.Message} in the Destination header");
                        }
                    }
                    catch
                    {
                        entities.Body.Dispose();
                        throw;
                    }

                default: throw new Exception("Unknown result type " + content.GetType());
            }
        }

        public bool IsCompliant(IRequest request, out string invalidReason)
        {
            invalidReason = null;
            return true;
        }
    }
}