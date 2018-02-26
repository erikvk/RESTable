using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using RESTar.Admin;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Results.Error;
using RESTar.Results.Error.BadRequest;
using RESTar.Results.Error.NotFound;
using RESTar.Results.Success;
using HttpRequest = RESTar.Http.HttpRequest;

namespace RESTar.Requests
{
    /// <inheritdoc />
    /// <summary>
    /// Contains the logic for the default RESTar protocol. This protocol is used if no 
    /// protocol indicator is included in the request URI.
    /// </summary>
    internal sealed class DefaultProtocolProvider : IProtocolProvider
    {
        /// <inheritdoc />
        public string ProtocolIdentifier { get; } = "restar";

        /// <inheritdoc />
        public void ParseQuery(string query, URI uri)
        {
            var match = Regex.Match(query, RegEx.RESTarRequestUri);
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
                    uri.ResourceSpecifier = EntityResource<AvailableResource>.ResourceSpecifier;
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

        public bool AllowExternalContentProviders { get; } = true;

        public IEnumerable<IContentTypeProvider> GetContentTypeProviders() => null;

        /// <inheritdoc />
        public string MakeRelativeUri(IUriParameters parameters)
        {
            string JoinConditions(List<UriCondition> c) => c.Count > 0 ? string.Join("&", c) : null;
            return $"/{parameters.ResourceSpecifier}" +
                   $"/{JoinConditions(parameters.Conditions) ?? "_"}" +
                   $"/{JoinConditions(parameters.MetaConditions) ?? "_"}";
        }

        /// <inheritdoc />
        public IFinalizedResult FinalizeResult(IResult result, ContentType accept, IContentTypeProvider contentTypeProvider)
        {
            switch (result)
            {
                case Report report:
                    result.Headers["RESTar-count"] = report.ReportBody.Count.ToString();
                    report.Body = contentTypeProvider.SerializeEntity(accept, report.ReportBody, report.Request);
                    report.ContentType = accept;
                    return report;

                case Entities entities:
                    entities.Body = contentTypeProvider.SerializeCollection(accept, entities.Content, entities.Request, out var entityCount);
                    if (entityCount == 0) return new NoContent(result);
                    entities.ContentType = accept;
                    entities.Headers["RESTar-count"] = entityCount.ToString();
                    entities.EntityCount = entityCount;
                    if (entities.IsPaged)
                    {
                        var pager = entities.GetNextPageLink();
                        entities.Headers["RESTar-pager"] = MakeRelativeUri(pager);
                    }
                    entities.SetContentDisposition(contentTypeProvider.GetContentDispositionFileExtension(accept));
                    if (entities.ExternalDestination != null)
                    {
                        try
                        {
                            var request = new HttpRequest(entities, entities.ExternalDestination)
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
                    return entities;

                case RESTarError error: return error;
                case var other: return (IFinalizedResult) other;
            }
        }

        /// <inheritdoc />
        public void CheckCompliance(Context context) { }
    }
}