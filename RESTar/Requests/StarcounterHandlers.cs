using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using RESTar.Linq;
using RESTar.Operations;
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
        private static readonly IDictionary<ulong, System.Action> OnConfirmationActions;

        static StarcounterHandlers()
        {
            WebSocketActions = new ConcurrentDictionary<ulong, Action<string, WebSocket>>();
            PreviousResult = new ConcurrentDictionary<ulong, IFinalizedResult>();
            OnConfirmationActions = new ConcurrentDictionary<ulong, System.Action>();
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
                    ws.SendContent(result);
                    return HandlerStatus.Handled;
                }));

            Handle.WebSocket(_Port, WsGroupName, (input, ws) => WebSocketActions.SafeGet(ws.ToUInt64())?.Invoke(input, ws));
            Handle.WebSocketDisconnect(_Port, WsGroupName, ws =>
            {
                var @ulong = ws.ToUInt64();
                WebSocketActions.Remove(@ulong);
                OnConfirmationActions.Remove(@ulong);
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
            if (OnConfirmationActions.TryGetValue(ws.ToUInt64(), out var action))
            {
                switch (input.FirstOrDefault())
                {
                    case var _ when input.Length > 1:
                    default:
                        ws.SendConfirmationRequest();
                        break;
                    case 'Y':
                    case 'y':
                        action();
                        OnConfirmationActions.Remove(ws.ToUInt64());
                        break;
                    case 'N':
                    case 'n':
                        OnConfirmationActions.Remove(ws.ToUInt64());
                        ws.SendCancel();
                        break;
                }
                return;
            }
            switch (input.FirstOrDefault())
            {
                case '\0':
                case '\n': break;
                case ' ' when input.Length == 1:
                    ws.SafeOperation(GET, ref query, null, headers, origin);
                    break;
                case '-':
                case '/':
                    query = input.Trim();
                    ws.SafeOperation(GET, ref query, null, headers, origin);
                    break;
                case '[':
                case '{':
                    ws.SafeOperation(POST, ref query, input.ToBytes(), headers, origin);
                    break;
                case var _ when input.Length > 2000:
                    ws.SendBadRequest();
                    break;
                default:
                    var (command, tail) = input.Trim().TSplit(' ');
                    switch (command.ToUpperInvariant())
                    {
                        case "GET":
                            if (!string.IsNullOrWhiteSpace(tail))
                                query = tail;
                            ws.SafeOperation(GET, ref query, null, headers, origin);
                            break;
                        case "POST":
                            ws.SafeOperation(POST, ref query, tail.ToBytes(), headers, origin);
                            break;
                        case "PUT":
                            ws.SendBadRequest("PUT is not available in the WebSocket interface");
                            break;
                        case "PATCH":
                            ws.UnsafeOperation(PATCH, query, tail.ToBytes(), headers, origin);
                            break;
                        case "DELETE":
                            ws.UnsafeOperation(DELETE, query, null, headers, origin);
                            break;
                        case "REPORT":
                            if (!string.IsNullOrWhiteSpace(tail))
                                query = tail;
                            ws.SafeOperation(REPORT, ref query, null, headers, origin);
                            break;
                        case "HELP":
                            ws.SendHelp();
                            break;
                        case "EXIT":
                        case "QUIT":
                        case "DISCONNECT":
                        case "CLOSE":
                            ws.Close();
                            break;
                        case "?":
                            ws.Send($"{(query.Any() ? query : "/")}");
                            break;
                        case "RELOAD":
                            ws.SafeOperation(GET, ref query, null, headers, origin);
                            break;
                        case "CREDITS":
                            ws.SendCredits();
                            break;
                        case var unknown:
                            ws.SendUnknownCommand(unknown);
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

        private static void SafeOperation(this WebSocket ws, Action action, ref string query, byte[] body, Headers headers, Origin origin)
        {
            ws.SendContent(ws.WsEvaluate(action, ref query, body, headers, origin));
        }

        private static void UnsafeOperation(this WebSocket ws, Action action, string query, byte[] body, Headers headers, Origin origin)
        {
            void operate()
            {
                headers.UnsafeOverride = true;
                var result = ws.WsEvaluate(action, ref query, body, headers, origin);
                ws.SendStatus(result);
            }

            var entities = PreviousResult.SafeGet(ws.ToUInt64()) as Entities;
            switch (entities?.EntityCount)
            {
                case null:
                case 0:
                    ws.SendBadRequest($". No entities for {action} operation. Make a selecting request before running {action}");
                    break;
                case 1:
                    operate();
                    break;
                case var many:
                    OnConfirmationActions[ws.ToUInt64()] = operate;
                    ws.SendConfirmationRequest($"This will run {action} on {many} entities in resource '{entities.Request.Resource.FullName}'. ");
                    break;
            }
        }

        private static void SendContent(this WebSocket ws, IFinalizedResult result)
        {
            switch (result)
            {
                case ConsoleInit _:
                    ws.Send("400: Bad request. Cannot enter the WebSocket console from another WebSocket");
                    break;
                case Report _:
                case Entities _:
                    ws.Send(result.Body.ToByteArray());
                    break;
                default:
                    ws.SendStatus(result);
                    break;
            }
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
            if (result is Forbidden)
                ws.Close();
        }

        private static void SendConfirmationRequest(this WebSocket ws, string initialInfo = null)
        {
            ws.Send($"{initialInfo}Type 'Y' to continue, 'N' to cancel");
        }

        private static void SendCancel(this WebSocket ws)
        {
            ws.Send("Operation cancelled");
        }

        private static void SendBadRequest(this WebSocket ws, string message = null)
        {
            ws.Send($"400: Bad request{message}");
        }

        private static void SendUnknownCommand(this WebSocket ws, string command)
        {
            ws.Send($"Unknown command '{command}'");
        }

        private static void Close(this WebSocket ws)
        {
            ws.Send("Closing RESTar WebSocket interface...");
            ws.Disconnect();
        }

        private static void SendHelp(this WebSocket ws)
        {
            ws.Send("### Welcome to the RESTar WebSocket interface! ###\n\n" +
                    "  The RESTar WebSocket interface makes it easy to send \n" +
                    "  multiple requests to the RESTar API, over a single \n" +
                    "  TCP connection. Using commands, the client can \n" +
                    "  navigate around the resources of the API, and read, \n" +
                    "  insert, update and/or delete entities. To navigate \n" +
                    "  and select entities, simply send a request URI over \n" +
                    "  the WebSocket, e.g. '/availableresource//limit=3'. \n" +
                    "  To insert an entity into a resource, send the JSON \n" +
                    "  representation over the WebSocket. To update entities,\n" +
                    "  send 'PATCH <json>', where <json> is the JSON data to \n" +
                    "  update entities from. To delete selected entities, send\n" +
                    "  'DELETE'. For potentially unsafe operations, you will be\n" +
                    "  asked to confirm before changes are applied.\n\n" +
                    "  Some other simple commands:\n" +
                    "  ?           Prints the current location \n" +
                    "  REPORT      Counts the entities at the current location\n" +
                    "  RELOAD      Relods the current location \n" +
                    "  HELP        Prints this help page \n" +
                    "  CLOSE       Closes the WebSocket\n");
        }

        private static void SendCredits(this WebSocket ws)
        {
            ws.Send("RESTar is designed and developed by Erik von Krusenstierna");
        }

        private static void SendConsoleInit(this WebSocket ws)
        {
            ws.Send("### Welcome to the RESTar WebSocket console! ###\n\n" +
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