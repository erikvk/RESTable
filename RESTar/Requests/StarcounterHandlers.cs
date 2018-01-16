using System;
using System.IO;
using System.Net;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Results.Success;
using RESTar.WebSockets;
using Starcounter;
using static RESTar.Admin.Settings;
using static RESTar.Requests.Action;
using static RESTar.Requests.RequestEvaluator;
using Console = RESTar.Admin.Console;

namespace RESTar.Requests
{
    internal static class StarcounterHandlers
    {
        private static readonly Action[] Actions = {GET, POST, PATCH, PUT, DELETE, REPORT, OPTIONS};
        private const string WsGroupName = "restar_ws";
        private static ulong RequestCount;

        internal static void RegisterRESTHandlers(bool setupMenu)
        {
            Actions.ForEach(action => Handle.CUSTOM
            (
                port: _Port,
                methodSpaceUri: $"{action} {_Uri}{{?}}",
                handler: (Request request, string query) =>
                {
                    var connection = GetTCPConnection(request);
                    var headers = new Headers(request.HeadersDictionary);
                    RequestCount += 1;
                    Console.LogHTTPRequest(RequestCount.ToString(), action, query, request.ClientIpAddress);
                    if (request.WebSocketUpgrade)
                        connection.WebSocket = new StarcounterWebSocket(WsGroupName, request, headers, connection);
                    var result = Evaluate(action, ref query, request.BodyBytes, headers, connection);
                    switch (result)
                    {
                        case WebSocketResult webSocketResult:
                            if (!webSocketResult.LeaveOpen)
                                connection.WebSocket.Disconnect();
                            return HandlerStatus.Handled;
                        default:
                            Console.LogHTTPResult(RequestCount.ToString(), result);
                            return result.ToResponse();
                    }
                }
            ));

            Handle.WebSocket(_Port, WsGroupName, (text, ws) =>
            {
                try
                {
                    WebSocketController.HandleTextInput(DbHelper.Base64ForUrlEncode(ws.ToUInt64()), text);
                }
                catch (Exception e)
                {
                    ws.Send("WebSocket error: " + e.Message);
                    ws.Disconnect();
                }
            });
            Handle.WebSocket(_Port, WsGroupName, (binary, ws) =>
            {
                try
                {
                    WebSocketController.HandleBinaryInput(DbHelper.Base64ForUrlEncode(ws.ToUInt64()), binary);
                }
                catch (Exception e)
                {
                    ws.Send("WebSocket error: " + e.Message);
                    ws.Disconnect();
                }
            });
            Handle.WebSocketDisconnect(_Port, WsGroupName, ws =>
            {
                try
                {
                    WebSocketController.HandleDisconnect(DbHelper.Base64ForUrlEncode(ws.ToUInt64()));
                }
                catch { }
            });

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
            response.SetHeadersDictionary(result.Headers._dict);
            return response;
        }

        private static TCPConnection GetTCPConnection(Request request)
        {
            var origin = new TCPConnection
            {
                Host = request.Host,
                Origin = request.IsExternal ? OriginType.External : OriginType.Internal,
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