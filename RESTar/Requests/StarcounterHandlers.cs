using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Results.Fail;
using RESTar.Results.Fail.Forbidden;
using RESTar.Results.Success;
using Starcounter;
using static RESTar.Admin.Settings;
using static RESTar.Requests.Action;
using static RESTar.Requests.RequestEvaluator;

namespace RESTar.Requests
{
    internal static class StarcounterHandlers
    {
        private static readonly Action[] Actions = {GET, POST, PATCH, PUT, DELETE, REPORT, OPTIONS};
        private static WebSocket Console;
        private static bool ConsoleActive;
        private static ulong RequestCount;
        private const string WsGroupName = "restar_ws";
        private const string ConsoleGroupName = "restar_console";
        private static readonly IDictionary<ulong, Action<string, WebSocket>> WebSocketActions;
        private static readonly IDictionary<ulong, IFinalizedResult> PreviousResult;
        private static readonly IDictionary<ulong, System.Action> ConfirmationActions;

        static StarcounterHandlers()
        {
            WebSocketActions = new ConcurrentDictionary<ulong, Action<string, WebSocket>>();
            PreviousResult = new ConcurrentDictionary<ulong, IFinalizedResult>();
            ConfirmationActions = new ConcurrentDictionary<ulong, System.Action>();
        }

        internal static void RegisterRESTHandlers(bool setupMenu)
        {
            Actions.ForEach(action => Handle.CUSTOM
            (
                port: _Port,
                methodSpaceUri: $"{action} {_Uri}{{?}}",
                handler: (Request request, string query) =>
                {
                    var origin = MakeOrigin(request);
                    var headers = new Headers(request.HeadersDictionary);
                    RequestCount += 1;
                    if (ConsoleActive)
                        Console.Send($"=> [{RequestCount}] {action} '{request.Uri}' from '{request.ClientIpAddress}'");
                    var result = Evaluate(action, ref query, request.BodyBytes, headers, origin);
                    if (ConsoleActive)
                        Console.Send($"<= [{RequestCount}] {result.StatusCode.ToCode()}: '{result.StatusDescription}'. " +
                                     $"{result.Headers["RESTar-info"]} {result.Headers["ErrorInfo"]}");
                    if (!request.WebSocketUpgrade)
                        return result.ToResponse();
                    if (result is ConsoleInit)
                    {
                        Console = request.SendUpgrade(ConsoleGroupName);
                        Console.SendConsoleInit();
                        return HandlerStatus.Handled;
                    }
                    WebSocketActions[request.GetWebSocketId()] = (input, _ws) => WebSocketShell(input, _ws, ref query, headers, origin);
                    var ws = request.SendUpgrade(WsGroupName);
                    PreviousResult[ws.ToUInt64()] = result;
                    ws.SendGETResult(result);
                    return HandlerStatus.Handled;
                }));

            Handle.WebSocket(_Port, WsGroupName, (input, ws) => WebSocketActions.SafeGet(ws.ToUInt64())?.Invoke(input, ws));
            Handle.WebSocketDisconnect(_Port, WsGroupName, ws =>
            {
                var @ulong = ws.ToUInt64();
                WebSocketActions.Remove(@ulong);
                ConfirmationActions.Remove(@ulong);
                PreviousResult.Remove(@ulong);
            });
            Handle.WebSocket(_Port, ConsoleGroupName, ConsoleShell);
            Handle.WebSocketDisconnect(_Port, ConsoleGroupName, ws => Console = null);

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

        private static void ConsoleShell(string input, WebSocket ws)
        {
            if (ws.ToUInt64() != Console.ToUInt64())
            {
                Console.Disconnect();
                Console = ws;
            }
            switch (input.ToUpperInvariant().Trim())
            {
                case "": break;
                case "BEGIN":
                    ConsoleActive = true;
                    Console.Send("Status: ACTIVE\n");
                    break;
                case "PAUSE":
                    ConsoleActive = false;
                    Console.Send("Status: PAUSED\n");
                    break;
                case "CLOSE":
                    Console.Send("Status: CLOSED\n");
                    Console.Disconnect();
                    Console = null;
                    break;
                case var unrecognized:
                    Console.SendUnknownCommand(unrecognized);
                    break;
            }
        }

        private static void WebSocketShell(string input, WebSocket ws, ref string query, Headers headers, Origin origin)
        {
            if (ConfirmationActions.TryGetValue(ws.ToUInt64(), out var action))
            {
                switch (input.ElementAtOrDefault(0))
                {
                    case 'Y':
                    case 'y':
                        action();
                        ConfirmationActions.Remove(ws.ToUInt64());
                        break;
                    case 'N':
                    case 'n':
                        ConfirmationActions.Remove(ws.ToUInt64());
                        ws.SendCancel();
                        break;
                    default:
                        ws.SendConfirmationRequest();
                        break;
                }
                return;
            }
            switch (input.ElementAtOrDefault(0))
            {
                case '\0':
                case '\n': break;
                case '-':
                case '/':
                    query = input.Trim();
                    ws.SendGET(ref query, headers, origin);
                    break;
                case '[':
                case '{':
                    ws.SendPOST(ref query, input.ToBytes(), headers, origin);
                    break;
                default:
                    if (input.Length > 2000) ws.SendBadRequest();
                    var (command, body) = input.Trim().TSplit(' ');
                    switch (command.ToUpperInvariant())
                    {
                        case "CLOSE":
                            ws.Close();
                            break;
                        case "?":
                            ws.Send($"{(query.Any() ? query : "/")}");
                            break;
                        case "RELOAD":
                            ws.SendGET(ref query, headers, origin);
                            break;
                        case "DELETE":
                            ws.SendDELETE(query, headers, origin);
                            break;
                        case "PATCH":
                            ws.SendPATCH(query, body.ToBytes(), headers, origin);
                            break;
                        case var other:
                            ws.SendUnknownCommand(other);
                            break;
                    }
                    break;
            }
        }

        #region Websockets extensions

        private static IFinalizedResult WsEvaluate(this WebSocket ws, Action action, ref string query, byte[] body, Headers headers, Origin origin)
        {
            return PreviousResult[ws.ToUInt64()] = Evaluate(action, ref query, body, headers, origin);
        }

        private static void SendGETResult(this WebSocket ws, IFinalizedResult result)
        {
            switch (result)
            {
                case ConsoleInit _:
                    ws.Send("400: Bad request. Cannot enter the WebSocket console from another WebSocket");
                    break;
                case Forbidden _:
                    ws.SendStatus(result);
                    ws.Close();
                    break;
                case RESTarError _:
                    ws.SendStatus(result);
                    break;
                case NoContent _:
                    ws.SendNoContent();
                    break;
                case Report _:
                case Entities _:
                    ws.Send(result.Body.ToByteArray());
                    break;
            }
        }

        private static void SendGET(this WebSocket ws, ref string query, Headers headers, Origin origin)
        {
            var result = ws.WsEvaluate(GET, ref query, null, headers, origin);
            ws.SendGETResult(result);
        }

        private static void SendPATCH(this WebSocket ws, string query, byte[] body, Headers headers, Origin origin)
        {
            void patch()
            {
                headers.UnsafeOverride = true;
                var result = ws.WsEvaluate(PATCH, ref query, body, headers, origin);
                ws.SendStatus(result);
            }

            var entities = PreviousResult.SafeGet(ws.ToUInt64()) as Entities;
            switch (entities?.EntityCount)
            {
                case null:
                case 0:
                    ws.SendBadRequest(". No entities to patch. Make a selecting request before running PATCH");
                    break;
                case 1:
                    patch();
                    break;
                case var many:
                    ConfirmationActions[ws.ToUInt64()] = patch;
                    ws.SendConfirmationRequest($"This will update {many} entities in resource '{entities.Request.Resource.FullName}'. ");
                    break;
            }
        }

        private static void SendDELETE(this WebSocket ws, string query, Headers headers, Origin origin)
        {
            void delete()
            {
                headers.UnsafeOverride = true;
                var result = ws.WsEvaluate(DELETE, ref query, null, headers, origin);
                ws.SendStatus(result);
            }

            var entities = PreviousResult.SafeGet(ws.ToUInt64()) as Entities;
            switch (entities?.EntityCount)
            {
                case null:
                case 0:
                    ws.SendBadRequest(". No entities to delete. Make a selecting request before running DELETE");
                    break;
                case 1:
                    delete();
                    break;
                case var many:
                    ConfirmationActions[ws.ToUInt64()] = delete;
                    ws.SendConfirmationRequest($"This will delete {many} entities from resource '{entities.Request.Resource.FullName}'. ");
                    break;
            }
        }

        private static void SendPOST(this WebSocket ws, ref string query, byte[] body, Headers headers, Origin origin)
        {
            var result = ws.WsEvaluate(POST, ref query, body, headers, origin);
            ws.SendStatus(result);
        }

        private static void SendConfirmationRequest(this WebSocket ws, string initialInfo = null)
        {
            ws.Send(initialInfo + "Type 'Y' to continue, 'N' to cancel");
        }

        private static void SendCancel(this WebSocket ws)
        {
            ws.Send("Operation cancelled");
        }

        private static void SendStatus(this WebSocket ws, IFinalizedResult result)
        {
            var info = result.Headers["RESTar-Info"];
            var errorInfo = result.Headers["ErrorInfo"];
            var tail = "";
            if (info != null)
                tail += $". {info}";
            if (errorInfo != null)
                tail += $". See {errorInfo}";
            ws.Send($"{result.StatusCode.ToCode()}: {result.StatusDescription}{tail}");
        }

        private static void SendNoContent(this WebSocket ws)
        {
            ws.Send("204: No content");
        }

        private static void SendBadRequest(this WebSocket ws, string message = null)
        {
            ws.Send("400: Bad request" + message);
        }

        private static void SendUnknownCommand(this WebSocket ws, string command)
        {
            ws.Send($"Unknown command '{command}'");
        }

        private static void Close(this WebSocket ws)
        {
            ws.Send("Now closing...");
            ws.Disconnect();
        }

        private static void SendConsoleInit(this WebSocket ws)
        {
            ws.Send("################################################\n" +
                    "### Welcome to the RESTar WebSocket console! ###\n" +
                    "################################################\n\n" +
                    ">>> Status: PAUSED\n\n" +
                    "> To begin, type BEGIN\n" +
                    "> To pause, type PAUSE\n" +
                    "> To close, type CLOSE\n");
        }

        #endregion

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