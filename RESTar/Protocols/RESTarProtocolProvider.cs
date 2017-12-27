using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RESTar.Admin;
using RESTar.Http;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Serialization;
using static RESTar.Internal.ErrorCodes;
using static Newtonsoft.Json.Formatting;
using static RESTar.Serialization.Serializer;
using static RESTar.Admin.Settings;

namespace RESTar.Protocols
{
    internal class RESTarProtocolProvider : IProtocolProvider
    {
        private static void PopulateFromUri(Arguments args, string uri)
        {
            if (uri.Count(c => c == '/') > 3) throw new InvalidSeparatorException();
            var match = Regex.Match(uri, RegEx.RESTarRequestUri);
            if (!match.Success) throw new SyntaxException(InvalidUriSyntax, "Check URI syntax");
            var resourceOrMacro = match.Groups["resource_or_macro"].Value.TrimStart('/');
            var view = match.Groups["view"].Value.TrimStart('-');
            var conditions = match.Groups["conditions"].Value.TrimStart('/');
            var metaConditions = match.Groups["metaconditions"].Value.TrimStart('/');

            switch (conditions)
            {
                case var _ when conditions.Length == 0:
                case var _ when conditions == "_": break;
                default:
                    args.UriConditions.AddRange(UriCondition.ParseMany(conditions, true));
                    break;
            }

            switch (metaConditions)
            {
                case var _ when metaConditions.Length == 0:
                case var _ when metaConditions == "_": break;
                default:
                    args.UriMetaConditions.AddRange(UriCondition.ParseMany(metaConditions, true));
                    break;
            }

            if (view.Length != 0)
                args.ViewName = view;

            switch (resourceOrMacro)
            {
                case var _ when resourceOrMacro.Length == 0:
                case var _ when resourceOrMacro == "_": break;
                case var resource when resourceOrMacro[0] != '$':
                    args.ResourceSpecifier = resource;
                    break;
                case var macro:
                    var macroName = macro.Substring(1);
                    args.Macro = DbMacro.Get(macroName) ?? throw new UnknownMacroException(macroName);
                    args.ResourceSpecifier = args.Macro.ResourceSpecifier;
                    args.ViewName = args.ViewName ?? args.Macro.ViewName;
                    var macroConditions = args.Macro.UriConditions;
                    if (macroConditions != null)
                        args.UriConditions.AddRange(macroConditions);
                    var macroMetaConditions = args.Macro.UriMetaConditions;
                    if (macroMetaConditions != null)
                        args.UriMetaConditions.AddRange(macroMetaConditions);
                    args.BodyBytes = args.BodyBytes ?? args.Macro.BodyBinary.ToArray();
                    if (args.Macro.Headers == null) break;
                    args.Macro.HeadersDictionary.ForEach(pair =>
                    {
                        var currentValue = args.Headers.SafeGet(pair.Key);
                        if (String.IsNullOrWhiteSpace(currentValue) || currentValue == "*/*")
                            args.Headers[pair.Key] = pair.Value;
                    });
                    break;
            }
        }

        private static string JoinConditions(ICollection<UriCondition> toJoin)
        {
            return toJoin.Count > 0 ? string.Join("$", toJoin) : null;
        }

        public string MakeRelativeUri(IUriParameters parameters) => $"/{parameters.ResourceSpecifier}" +
                                                                    $"/{JoinConditions(parameters.UriConditions) ?? "_"}" +
                                                                    $"/{JoinConditions(parameters.UriMetaConditions) ?? "_"}";

        public IFinalizedResult FinalizeResult(Result result)
        {
            if (result.Entities != null)
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

                        if (result.HasContent)
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
                    var response = request.GetResponse() ?? throw new DestinationException(request, "No response");
                    if (!response.IsSuccessStatusCode)
                        throw new DestinationException(request,
                            $"Received {response.StatusCode.ToCode()} - {response.StatusDescription}. {response.Headers.SafeGet("RESTar-info")}");
                    if (result.Headers.TryGetValue("Access-Control-Allow-Origin", out var h))
                        response.Headers["Access-Control-Allow-Origin"] = h;
                    return response;
                }
                catch (HttpRequestException re)
                {
                    throw new SyntaxException(InvalidDestination, $"{re.Message} in the Destination header");
                }
            }

            return result;
        }

        public Arguments MakeRequestArguments(string uri, byte[] body = null, IDictionary<string, string> headers = null,
            string contentType = null, string accept = null)
        {
            var args = new Arguments
            {
                BodyBytes = body,
                Headers = headers ?? new Dictionary<string, string>(),
                ContentType = contentType ?? MimeTypes.JSON,
                Accept = accept ?? MimeTypes.JSON,
            };
            PopulateFromUri(args, uri);
            return args;
        }
    }
}