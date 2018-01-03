﻿using System.IO;
using System.Net;
using RESTar.Linq;
using RESTar.Operations;
using Starcounter;
using static RESTar.Admin.Settings;
using static RESTar.Requests.Action;
using static RESTar.Requests.RequestEvaluator;

namespace RESTar.Requests
{
    internal static class StarcounterHandlers
    {
        private static readonly Action[] Actions = {GET, POST, PATCH, PUT, DELETE, REPORT, OPTIONS};

        internal static void RegisterRESTHandlers(bool setupMenu)
        {
            Actions.ForEach(action => Handle.CUSTOM
            (
                port: _Port,
                methodSpaceUri: $"{action} {_Uri}{{?}}",
                handler: (Request request, string query) => Evaluate
                (
                    action: action,
                    query: query,
                    body: request.BodyBytes,
                    headers: request.HeadersDictionary,
                    origin: MakeOrigin(request)
                ).ToResponse()
            ));

            #region View

            // if (!_ViewEnabled) return;
            // Application.Current.Use(new HtmlFromJsonProvider());
            // Application.Current.Use(new PartialToStandaloneHtmlProvider());
            // var appName = Application.Current.Name;
            // Handle.GET($"/{appName}{{?}}", (Request request, string query) => Evaluate(VIEW, () => MakeArgs(request, query)).ToResponse());
            // Handle.GET("/__restar/__page", () => Evaluate(PAGE).ToResponse());
            // if (!setupMenu) return;
            // Handle.GET($"/{appName}", () => Evaluate(MENU).ToResponse());

            #endregion
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

        private static Origin MakeOrigin(Request request)
        {
            var origin = new Origin
            {
                Host = request.Host,
                Type = request.IsExternal ? OriginType.External : OriginType.Internal,
                ClientIP = request.ClientIpAddress
            };

            if (origin.IsExternal && request.HeadersDictionary != null)
            {
                origin.HTTPS = request.HeadersDictionary.ContainsKey("X-ARR-SSL") || request.HeadersDictionary.ContainsKey("X-HTTPS");
                if (request.HeadersDictionary.TryGetValue("X-Forwarded-For", out var ip))
                {
                    origin.ClientIP = IPAddress.Parse(ip.Split(':')[0]);
                    origin.ProxyIP = request.ClientIpAddress;
                }
            }
            return origin;
        }

        internal static void UnregisterRESTHandlers()
        {
            void UnregisterREST(Action action) => Handle.UnregisterHttpHandler(_Port, $"{action}", $"{_Uri}{{?}}");
            Actions.ForEach(action => Do.Try(() => UnregisterREST(action)));
            var appName = Application.Current.Name;
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", $"/{appName}{{?}}"));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", "/__restar/__page"));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", $"/{appName}"));
        }
    }
}