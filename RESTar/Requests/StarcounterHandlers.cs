using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Operations;
using Starcounter;

namespace RESTar.Requests
{
    internal static class StarcounterHandlers
    {
        private static void Register(Methods method, Func<HandlerActions, Func<Arguments>, IFinalizedResult> func) => Handle.CUSTOM
        (
            port: Settings._Port,
            methodSpaceUri: $"{method} {Settings._Uri}{{?}}",
            handler: (Request request, string query) => func((HandlerActions) method, () => ToArgs(request, query)).ToResponse()
        );

        internal static void RegisterRESTHandlers(bool setupMenu)
        {
            RESTarConfig.Methods.ForEach(method => Register(method, Evaluator.Evaluate));

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

        private static Arguments ToArgs(Request request, string query) => Protocols.Protocol.MakeArguments
        (
            uri: query,
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
            var uri = Settings._Uri + "{?}";
            Do.Try(() => Handle.UnregisterHttpHandler(Settings._Port, "GET", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(Settings._Port, "POST", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(Settings._Port, "PUT", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(Settings._Port, "PATCH", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(Settings._Port, "DELETE", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(Settings._Port, "OPTIONS", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(Settings._Port, "REPORT", uri));
            var appName = Application.Current.Name;
            Do.Try(() => Handle.UnregisterHttpHandler(Settings._Port, "GET", $"/{appName}{{?}}"));
            Do.Try(() => Handle.UnregisterHttpHandler(Settings._Port, "GET", "/__restar/__page"));
            Do.Try(() => Handle.UnregisterHttpHandler(Settings._Port, "GET", $"/{appName}"));
        }
    }
}