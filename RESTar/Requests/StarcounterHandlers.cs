using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Operations;
using Starcounter;
using static RESTar.Requests.HandlerActions;
using static RESTar.Admin.Settings;
using static RESTar.Requests.Evaluator;

namespace RESTar.Requests
{
    internal static class StarcounterHandlers
    {
        internal static void RegisterRESTHandlers(bool setupMenu)
        {
            var uri = _Uri + "{?}";
            Handle.GET(_Port, uri, (Request request, string _) => Evaluate(GET, request.ToArgs).ToResponse());
            Handle.POST(_Port, uri, (Request request, string _) => Evaluate(POST, request.ToArgs).ToResponse());
            Handle.PUT(_Port, uri, (Request request, string _) => Evaluate(PUT, request.ToArgs).ToResponse());
            Handle.PATCH(_Port, uri, (Request request, string _) => Evaluate(PATCH, request.ToArgs).ToResponse());
            Handle.DELETE(_Port, uri, (Request request, string _) => Evaluate(DELETE, request.ToArgs).ToResponse());
            Handle.CUSTOM(_Port, $"REPORT {uri}", (Request request, string _) => Evaluate(COUNT, request.ToArgs).ToResponse());
            Handle.OPTIONS(_Port, uri, (Request request, string _) => Evaluate(ORIGIN, request.ToArgs).ToResponse());
            // if (!_ViewEnabled) return;
            // Application.Current.Use(new HtmlFromJsonProvider());
            // Application.Current.Use(new PartialToStandaloneHtmlProvider());
            // var appName = Application.Current.Name;
            // Handle.GET($"/{appName}{{?}}", (Request request, string query) => Evaluate(VIEW, () => MakeArgs(request, query)).ToResponse());
            // Handle.GET("/__restar/__page", () => Evaluate(PAGE).ToResponse());
            // if (!setupMenu) return;
            // Handle.GET($"/{appName}", () => Evaluate(MENU).ToResponse());
        }

        private static Response ToResponse(this IFinalizedResult result)
        {
            var response = new Response
            {
                StatusCode = (ushort) result.StatusCode,
                StatusDescription = result.StatusDescription,
                ContentType = result.ContentType ?? MimeTypes.JSON
            };
            if (result.Body != null)
            {
                if (result.Body.CanSeek && result.Body.Length > 0)
                    response.StreamedBody = result.Body;
                else
                {
                    var stream = new MemoryStream();
                    result.Body.CopyTo(stream);
                    if (stream.Position > 0)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        response.StreamedBody = stream;
                    }
                }
            }
            response.SetHeadersDictionary(result.Headers);
            return response;
        }

        private static Arguments ToArgs(this Request request) => Protocol.Protocol.MakeRequestArguments
        (
            uri: request.Uri,
            body: request.BodyBytes,
            headers: request.HeadersDictionary ?? new Dictionary<string, string>(),
            contentType: request.ContentType,
            accept: request.PreferredMimeTypeString,
            origin: MakeOrigin(request)
        );

        private static Origin MakeOrigin(Request request)
        {
            var origin = new Origin();
            if (request == null)
            {
                origin.Type = OriginType.Internal;
                origin.IP = null;
                origin.Proxy = null;
            }
            else
            {
                string ip = null;
                if (request.HeadersDictionary?.TryGetValue("X-Forwarded-For", out ip) == true && ip != null)
                {
                    origin.IP = IPAddress.Parse(ip.Split(':')[0]);
                    origin.Proxy = request.ClientIpAddress;
                }
                else
                {
                    origin.IP = request.ClientIpAddress;
                    origin.Proxy = null;
                }
                origin.Type = request.IsExternal ? OriginType.External : OriginType.Internal;
            }
            return origin;
        }

        internal static void UnRegisterRESTHandlers()
        {
            var uri = _Uri + "{?}";
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "POST", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "PUT", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "PATCH", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "DELETE", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "OPTIONS", uri));
            var appName = Application.Current.Name;
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", $"/{appName}{{?}}"));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", "/__restar/__page"));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", $"/{appName}"));
        }
    }
}