using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using RESTar.Linq;
using RESTar.NetworkProviders;
using RESTar.Requests;
using RESTar.Resources.Operations;
using RESTar.Results;
using RESTar.WebSockets;
using Starcounter;
using Console = RESTar.Admin.Console;
using ScRequest = Starcounter.Request;

namespace RESTar.Internal.Sc
{
    internal class ScNetworkProvider : INetworkProvider
    {
        internal const string WsGroupName = "restar_ws";

        public void AddBindings(Method[] methods, string rootUri, ushort port)
        {
            methods.ForEach(method => Handle.CUSTOM
            (
                port: port,
                methodSpaceUri: $"{method} {rootUri}{{?}}",
                handler: (ScRequest scRequest, string uri) =>
                {
                    var headers = new Headers(scRequest.HeadersDictionary);
                    var client = GetClient(scRequest);
                    if (!client.TryAuthenticate(ref uri, headers, out var error))
                        return ToResponse(error);
                    var context = new ScContext(client, scRequest);
                    using (var request = context.CreateRequest(method, uri, scRequest.BodyBytes, headers))
                    {
                        switch (request.Evaluate().Serialize())
                        {
                            case WebSocketUpgradeSuccessful _: return HandlerStatus.Handled;
                            case var result:
                                Console.Log(request, result);
                                return ToResponse(result);
                        }
                    }
                }
            ));

            Handle.OPTIONS
            (
                port: port,
                uriTemplate: $"{rootUri}{{?}}",
                handler: (ScRequest scRequest, string query) =>
                {
                    var context = new ScContext(GetClient(scRequest), scRequest);
                    var headers = new Headers(scRequest.HeadersDictionary);
                    return ToResponse(context.CheckOrigin(query, headers));
                }
            );

            Handle.WebSocket(port, WsGroupName, (text, ws) =>
            {
                try
                {
                    WebSocketController.HandleTextInput(DbHelper.Base64EncodeObjectNo(ws.ToUInt64()), text).Wait();
                }
                catch (Exception e)
                {
                    ws.Send(e.Message);
                    ws.Disconnect();
                }
            });

            Handle.WebSocket(port, WsGroupName, (binary, ws) =>
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

            Handle.WebSocketDisconnect(port, WsGroupName, ws =>
            {
                try
                {
                    WebSocketController.HandleDisconnect(DbHelper.Base64EncodeObjectNo(ws.ToUInt64()));
                }
                catch { }
            });
        }

        public void RemoveBindings(Method[] methods, string rootUri, ushort port) => methods
            .ForEach(method => Do.Try(() => Handle.UnregisterHttpHandler(port, $"{method}", $"{rootUri}{{?}}")));

        private static Response ToResponse(ISerializedResult result)
        {
            var response = new Response
            {
                StatusCode = (ushort) result.StatusCode,
                StatusDescription = result.StatusDescription
            };
            if (result.Body != null)
            {
                if (result.Headers.ContentType.HasValue)
                    response.ContentType = result.Headers.ContentType.ToString();
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

        private static Client GetClient(ScRequest request)
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
    }
}