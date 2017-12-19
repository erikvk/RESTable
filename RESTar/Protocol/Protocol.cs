using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using RESTar.Admin;
using RESTar.Http;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Serialization;
using static RESTar.Admin.Settings;

namespace RESTar.Protocol
{
    internal static class Protocol
    {
        internal static Arguments MakeRequestArguments
        (
            string uri,
            byte[] body,
            Dictionary<string, string> headers,
            string contentType,
            string accept,
            Origin origin
        )
        {
            var protocol = (headers.TryGetNoCase("odata-version", out var v) ||
                            headers.TryGetNoCase("odata-maxversion", out v)) && v == "4.0"
                ? Protocols.OData
                : Protocols.RESTar;
            switch (protocol)
            {
                case Protocols.RESTar: return RESTar.MakeRequestArguments(uri, body, headers, contentType, accept, origin);
                case Protocols.OData: return OData.MakeRequestArguments(uri, body, headers, contentType, accept, origin);
                default: return null;
            }
        }

        internal static class RESTar
        {
            private static IFinalizedResult FinalizeResult(Result result)
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
                        throw new SyntaxException(ErrorCodes.InvalidDestination, $"{re.Message} in the Destination header");
                    }
                }

                return result;
            }

            private static void PopulateFromUri(Arguments args, string uri)
            {
                uri = uri.Remove(0, _Uri.Length);
                if (uri.Count(c => c == '/') > 3) throw new InvalidSeparatorException();
                if (args.PercentCharsEscaped) uri = uri.Replace("%25", "%");
                var uriGroups = Regex.Match(uri, RegEx.RESTarRequestUri).Groups;
                var resourceOrMacro = uriGroups["resource_or_macro"].Value.TrimStart('/');
                var view = uriGroups["view"].Value.TrimStart('-');
                var conditions = uriGroups["conditions"].Value.TrimStart('/');
                var metaConditions = uriGroups["metaconditions"].Value.TrimStart('/');

                if (conditions.Any())
                    args.UriConditions.AddRange(UriCondition.ParseMany(conditions, true));
                if (metaConditions.Any())
                    args.UriMetaConditions.AddRange(UriCondition.ParseMany(metaConditions, true));
                if (view.Any())
                    args.ViewName = view;
                if (!resourceOrMacro.Any()) return;
                if (resourceOrMacro[0] != '$')
                    args.ResourceSpecifier = resourceOrMacro;
                else
                {
                    var macroString = resourceOrMacro.Substring(1);
                    args.Macro = DbMacro.Get(macroString) ?? throw new UnknownMacroException(macroString);
                    args.ResourceSpecifier = args.Macro.ResourceSpecifier;
                    args.ViewName = args.ViewName ?? args.Macro.ViewName;
                    var macroConditions = args.Macro.UriConditions;
                    if (macroConditions != null)
                        args.UriConditions.AddRange(macroConditions);
                    var macroMetaConditions = args.Macro.UriMetaConditions;
                    if (macroMetaConditions != null)
                        args.UriMetaConditions.AddRange(macroMetaConditions);
                    args.BodyBytes = args.BodyBytes ?? args.Macro.BodyBinary.ToArray();
                    if (args.Macro.Headers == null) return;
                    foreach (var pair in JObject.Parse(args.Macro.Headers))
                    {
                        var currentValue = args.Headers[pair.Key];
                        if (string.IsNullOrWhiteSpace(currentValue) || currentValue == "*/*")
                            args.Headers[pair.Key] = pair.Value.Value<string>();
                    }
                }
            }

            internal static Arguments MakeRequestArguments(string uri, byte[] body = null,
                IDictionary<string, string> headers = null, string contentType = null, string accept = null, Origin origin = null)
            {
                var args = new Arguments
                {
                    Origin = origin ?? Origin.Internal,
                    BodyBytes = body,
                    Headers = headers ?? new Dictionary<string, string>(),
                    ContentType = contentType ?? MimeTypes.JSON,
                    Accept = accept ?? MimeTypes.JSON,
                    ResultFinalizer = FinalizeResult
                };
                PopulateFromUri(args, uri);
                return args;
            }
        }

        private static class OData
        {
            private static IFinalizedResult FinalizeResult(Result result)
            {
                return null;
            }

            private static void PopulateFromUri(Arguments args, string uri)
            {
                //
            }

            internal enum MetaDataLevel
            {
                None,
                Minimal,
                All
            }

            internal static Arguments MakeRequestArguments(string uri, byte[] body, IDictionary<string, string> headers,
                string contentType,
                string accept, Origin origin)
            {
                var accept_metadata = accept.Split(';');
                accept = accept_metadata[0];
                var args = new Arguments
                {
                    Origin = origin,
                    BodyBytes = body,
                    Headers = headers,
                    ContentType = contentType,
                    Accept = accept,
                    ResultFinalizer = FinalizeResult
                };
                PopulateFromUri(args, uri);
                return args;
            }
        }
    }
}