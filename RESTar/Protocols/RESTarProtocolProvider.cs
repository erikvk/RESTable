using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Admin;
using RESTar.Http;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Serialization;
using static RESTar.Internal.ErrorCodes;
using static Newtonsoft.Json.Formatting;
using static RESTar.Serialization.Serializer;
using static RESTar.Admin.Settings;

namespace RESTar.Protocols
{
    internal class RESTarProtocolProvider
    {
        internal static void PopulateUri(URI uri, string query)
        {
            var match = Regex.Match(query, RegEx.RESTarRequestUri);
            if (!match.Success) throw new InvalidSyntax(InvalidUriSyntax, "Check URI syntax");

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
            if (result.HasEntities)
            {
                switch (result.ContentType)
                {
                    case MimeTypes.JSON:
                        var stream = new MemoryStream();
                        var formatter = result.Request.MetaConditions.Formatter;
                        using (var swr = new StreamWriter(stream, UTF8, 1024, true))
                        using (var jwr = new RESTarJsonWriter(swr, formatter.StartIndent))
                        {
                            JsonSerializer.Formatting = _PrettyPrint ? Indented : None;
                            swr.Write(formatter.Pre);
                            JsonSerializer.Serialize(jwr, result.Entities);
                            result.EntityCount = jwr.ObjectsWritten;
                            swr.Write(formatter.Post);
                        }

                        if (result.HasEntities)
                        {
                            result.Body = stream;
                            result.SetContentDisposition(".json");
                        }

                        break;

                    case MimeTypes.Excel:
                        result.Body = null;
                        var excel = result.Entities.ToExcel(result.Request.Resource);
                        if (excel != null)
                        {
                            result.EntityCount = excel.Worksheet(1).RowsUsed().Count() - 1;
                            result.Body = new MemoryStream();
                            excel.SaveAs(result.Body);
                            result.SetContentDisposition(".xlsx");
                        }

                        break;
                }

                result.Body?.Seek(0, SeekOrigin.Begin);
                result.Headers["RESTar-count"] = result.EntityCount.ToString();
                if (result.IsPaged)
                {
                    var pager = result.GetNextPageLink();
                    result.Headers["RESTar-pager"] = MakeRelativeUri(pager);
                }
            }

            if (result.ExternalDestination != null)
            {
                try
                {
                    var request = new HttpRequest(result.ExternalDestination)
                    {
                        ContentType = result.ContentType,
                        AuthToken = result.Request.AuthToken,
                        Body = result.Body
                    };
                    var response = request.GetResponse() ?? throw new InvalidExternalDestination(request, "No response");
                    if (!response.IsSuccessStatusCode)
                        throw new InvalidExternalDestination(request,
                            $"Received {response.StatusCode.ToCode()} - {response.StatusDescription}. {response.Headers.SafeGet("RESTar-info")}");
                    if (result.Headers.TryGetValue("Access-Control-Allow-Origin", out var h))
                        response.Headers["Access-Control-Allow-Origin"] = h;
                    return response;
                }
                catch (HttpRequestException re)
                {
                    throw new InvalidSyntax(InvalidDestination, $"{re.Message} in the Destination header");
                }
            }

            return result;
        }
    }
}