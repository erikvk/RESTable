using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Admin;
using RESTar.Http;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Serialization;
using static RESTar.Internal.ErrorCodes;

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
                        if (string.IsNullOrWhiteSpace(currentValue) || currentValue == "*/*")
                            args.Headers[pair.Key] = pair.Value;
                    });
                    break;
            }
        }

        public IFinalizedResult FinalizeResult(Result result)
        {
            if (result.Entities != null)
            {
                var (stream, count, hasContent, extension) = default((MemoryStream, long, bool, string));
                switch (result.ContentType)
                {
                    case MimeTypes.JSON:
                        var formatter = result.Request.MetaConditions.Formatter;
                        hasContent = result.Entities.SerializeOutputJsonRESTar(formatter, out stream, out count);
                        extension = ".json";
                        break;
                    case MimeTypes.Excel:
                        hasContent = result.Entities.SerializeOutputExcel(result.Request.Resource, out stream, out count);
                        extension = ".xlsx";
                        break;
                }

                if (hasContent)
                {
                    result.Body = stream;
                    result.Headers["RESTar-count"] = count.ToString();
                    result.Headers["Content-Disposition"] = $"attachment; filename={result.Request.Resource.Name}_" +
                                                            $"{DateTime.Now:yyMMddHHmmssfff}{extension}";
                    if (count == result.Request.MetaConditions.Limit)
                        result.Headers["RESTar-pager"] = $"limit={result.Request.MetaConditions.Limit}&" +
                                                         $"offset={result.Request.MetaConditions.Offset + count}";
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