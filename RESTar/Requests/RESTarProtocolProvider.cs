using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Admin;
using RESTar.Http;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Results.Error;
using RESTar.Results.Fail;
using RESTar.Results.Fail.BadRequest;
using RESTar.Results.Fail.NotFound;
using RESTar.Results.Success;
using RESTar.Serialization;
using static Newtonsoft.Json.Formatting;
using static RESTar.Admin.Settings;
using static RESTar.Serialization.Serializer;

namespace RESTar.Requests
{
    internal class RESTarProtocolProvider
    {
        internal static void PopulateUri(URI uri, string query)
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
                case var _ when resourceOrMacro.Length == 0:
                case var _ when resourceOrMacro == "_": break;
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

        private static string JoinConditions(ICollection<UriCondition> toJoin) => toJoin.Count > 0 ? string.Join("&", toJoin) : null;

        internal static string MakeRelativeUri(IUriParameters parameters) => $"/{parameters.ResourceSpecifier}" +
                                                                             $"/{JoinConditions(parameters.Conditions) ?? "_"}" +
                                                                             $"/{JoinConditions(parameters.MetaConditions) ?? "_"}";

        internal static IFinalizedResult FinalizeResult(Result result)
        {
            if (!(result is Entities entities)) return result;
            var accept = entities.Request.Accept;
            switch (accept.TypeCode)
            {
                case MimeTypeCode.Json:
                    var stream = new MemoryStream();
                    var formatter = entities.Request.MetaConditions.Formatter;
                    using (var swr = new StreamWriter(stream, UTF8, 1024, true))
                    using (var jwr = new RESTarJsonWriter(swr, formatter.StartIndent))
                    {
                        Json.Formatting = _PrettyPrint ? Indented : None;
                        swr.Write(formatter.Pre);
                        Json.Serialize(jwr, entities.Content);
                        entities.EntityCount = jwr.ObjectsWritten;
                        swr.Write(formatter.Post);
                    }
                    if (entities.EntityCount > 0)
                    {
                        entities.ContentType = MimeTypes.JSON;
                        entities.Body = stream;
                        entities.SetContentDisposition(".json");
                    }
                    else return new NoContent();
                    if (entities.IsPaged)
                    {
                        var pager = entities.GetNextPageLink();
                        entities.Headers["RESTar-pager"] = MakeRelativeUri(pager);
                    }
                    break;

                case MimeTypeCode.Excel:
                    entities.Body = null;
                    var excel = entities.Content.ToExcel(entities.Request.Resource);
                    if (excel != null)
                    {
                        entities.ContentType = MimeTypes.Excel;
                        var count = excel.Worksheet(1).RowsUsed().Count();
                        if (count > 0)
                            entities.EntityCount = (ulong) (count - 1);
                        entities.Body = new MemoryStream();
                        excel.SaveAs(entities.Body);
                        entities.SetContentDisposition(".xlsx");
                    }
                    else return new NoContent();
                    break;
            }
            entities.Body?.Seek(0, SeekOrigin.Begin);
            entities.Headers["RESTar-count"] = entities.EntityCount.ToString();
            if (entities.ExternalDestination != null)
            {
                try
                {
                    var request = new HttpRequest(entities.ExternalDestination)
                    {
                        ContentType = entities.ContentType,
                        AuthToken = entities.Request.AuthToken,
                        Body = entities.Body
                    };
                    var response = request.GetResponse() ?? throw new InvalidExternalDestination(request, "No response");
                    if (!response.IsSuccessStatusCode)
                        throw new InvalidExternalDestination(request,
                            $"Received {response.StatusCode.ToCode()} - {response.StatusDescription}. {response.Headers.SafeGet("RESTar-info")}");
                    if (entities.Headers.TryGetValue("Access-Control-Allow-Origin", out var h))
                        response.Headers["Access-Control-Allow-Origin"] = h;
                    return response;
                }
                catch (HttpRequestException re)
                {
                    throw new InvalidSyntax(ErrorCodes.InvalidDestination, $"{re.Message} in the Destination header");
                }
            }
            return entities;
        }
    }
}