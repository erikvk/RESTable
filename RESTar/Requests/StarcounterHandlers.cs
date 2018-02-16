using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Results.Success;
using RESTar.WebSockets;
using Starcounter;
using static RESTar.Admin.Settings;
using static RESTar.Requests.Action;
using static RESTar.Requests.RequestEvaluator;

namespace RESTar.Requests
{
    internal static class StarcounterHandlers
    {
        private static readonly Action[] Actions = {GET, POST, PATCH, PUT, DELETE, REPORT, OPTIONS, HEAD};
        private const string WsGroupName = "restar_ws";

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
                    if (request.WebSocketUpgrade)
                        connection.WebSocket = new StarcounterWebSocket(WsGroupName, request, headers, connection);
                    var result = Evaluate(action, ref query, request.BodyBytes, headers, connection);
                    if (result is WebSocketResult webSocketResult)
                    {
                        if (!webSocketResult.LeaveOpen)
                            connection.WebSocket.Disconnect();
                        return HandlerStatus.Handled;
                    }
                    return result.ToResponse();
                }
            ));

            Handle.WebSocket(_Port, WsGroupName, (text, ws) =>
            {
                try
                {
                    WebSocketController.HandleTextInput(DbHelper.Base64EncodeObjectNo(ws.ToUInt64()), text);
                }
                catch (Exception e)
                {
                    ws.Send(e.Message);
                    ws.Disconnect();
                }
            });
            Handle.WebSocket(_Port, WsGroupName, (binary, ws) =>
            {
                try
                {
                    WebSocketController.HandleBinaryInput(DbHelper.Base64EncodeObjectNo(ws.ToUInt64()), binary);
                }
                catch (Exception e)
                {
                    ws.Send(e.Message);
                    ws.Disconnect();
                }
            });
            Handle.WebSocketDisconnect(_Port, WsGroupName, ws =>
            {
                try
                {
                    WebSocketController.HandleDisconnect(DbHelper.Base64EncodeObjectNo(ws.ToUInt64()));
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
            result.Headers.ForEach(header => response.Headers[header.Key] = header.Value);
            response.Cookies = result.Cookies as List<string> ?? response.Cookies.ToList();
            return response;
        }

        private static TCPConnection GetTCPConnection(Request request)
        {
            var origin = new TCPConnection
            {
                Host = request.Host,
                Origin = request.IsExternal ? OriginType.External : OriginType.Internal,
                ClientIP = request.ClientIpAddress,
                UserAgent = request.Headers["User-Agent"]
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