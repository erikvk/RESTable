using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using RESTar.Deflection;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Results.Success;
using RESTar.WebSockets;
using Starcounter;
using static RESTar.Admin.Settings;

namespace RESTar.Requests
{
    internal static class StarcounterHandlers
    {
        private const string WsGroupName = "restar_ws";

        internal static void RegisterRESTHandlers()
        {
            RESTarConfig.Methods.ForEach(method => Handle.CUSTOM
            (
                port: _Port,
                methodSpaceUri: $"{method} {_Uri}{{?}}",
                handler: (Starcounter.Request scRequest, string query) =>
                {
                    using (var client = GetClient(scRequest))
                    {
                        var headers = new Headers(scRequest.HeadersDictionary);
                        if (scRequest.WebSocketUpgrade)
                            client.WebSocket = new StarcounterWebSocket(WsGroupName, scRequest, headers, client);
                        var stopwatch = Stopwatch.StartNew();
                        var request = Request.Create(client, method, ref query, scRequest.BodyBytes, headers);
                        var result = request.GetResult().FinalizeResult();
                        stopwatch.Stop();
                        if (result is WebSocketResult webSocketResult)
                        {
                            if (!webSocketResult.LeaveOpen)
                                client.WebSocketInternal.Disconnect();
                            return HandlerStatus.Handled;
                        }
                        Admin.Console.Log(request, result, stopwatch.Elapsed.TotalMilliseconds);
                        return result.ToResponse();
                    }
                }
            ));

            Handle.OPTIONS
            (
                port: _Port,
                uriTemplate: $"{_Uri}{{?}}",
                handler: (Starcounter.Request scRequest, string query) =>
                {
                    using (var client = GetClient(scRequest))
                    {
                        var headers = new Headers(scRequest.HeadersDictionary);
                        return Request.CheckOrigin(client, ref query, headers).ToResponse();
                    }
                }
            );

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
                StatusDescription = result.StatusDescription
            };
            if (result.Body != null)
            {
                response.ContentType = result.ContentType.ToString();
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

        private static Client GetClient(Starcounter.Request request)
        {
            var clientIP = request.ClientIpAddress;
            var proxyIP = default(IPAddress);
            var host = request.Host;
            var userAgent = request.Headers["User-Agent"];
            var https = false;
            if (request.HeadersDictionary != null)
            {
                https = request.HeadersDictionary.ContainsKey("X-ARR-SSL") || request.HeadersDictionary.ContainsKey("X-HTTPS");
                if (request.HeadersDictionary.TryGetValue("X-Forwarded-For", out var ip))
                {
                    clientIP = IPAddress.Parse(ip.Split(':')[0]);
                    proxyIP = request.ClientIpAddress;
                }
            }
            return Client.External(clientIP, proxyIP, userAgent, host, https);
        }

        internal static void UnregisterRESTHandlers()
        {
            void UnregisterREST(Methods method) => Handle.UnregisterHttpHandler(_Port, $"{method}", $"{_Uri}{{?}}");
            EnumMember<Methods>.Values.ForEach(method => Do.Try(() => UnregisterREST(method)));
            var appName = Application.Current.Name;
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", $"/{appName}{{?}}"));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", "/__restar/__page"));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", $"/{appName}"));
        }
    }
}