using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Linq;
using RESTar.Operations;
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

        internal static void RegisterRESTHandlers(bool setupMenu)
        {
            Actions.ForEach(action => Handle.CUSTOM
            (
                port: _Port,
                methodSpaceUri: $"{action} {_Uri}{{?}}",
                handler: (Request request, string query) =>
                {
                    var origin = MakeOrigin(request);
                    RequestCount += 1;
                    if (ConsoleActive)
                        Console.Send($"=> [{RequestCount}] {action} '{request.Uri}' from '{request.ClientIpAddress}'\n");
                    var result = Evaluate(action, query, request.BodyBytes, request.HeadersDictionary, origin);
                    if (ConsoleActive)
                        Console.Send($"<= [{RequestCount}] {result.StatusCode.ToCode()}: '{result.StatusDescription}'. " +
                                     $"{result.Headers["RESTar-info"]} {result.Headers["ErrorInfo"]}\n");
                    if (!request.WebSocketUpgrade)
                        return result.ToResponse();
                    if (result is ConsoleInit)
                    {
                        Console = request.SendUpgrade("restar_console");
                        Console.SendConsoleInit();
                        return HandlerStatus.Handled;
                    }
                    var socket = request.SendUpgrade("restar_ws");
                    socket.SendGETResult(result);
                    Handle.WebSocket(_Port, "restar_ws", (input, _socket) =>
                    {
                        switch (input[0])
                        {
                            case '\n': break;
                            case '/':
                                query = input.Trim();
                                _socket.SendGET(query, request.HeadersDictionary, origin);
                                break;
                            case '[':
                            case '{':
                                _socket.SendPOST(query, input.ToBytes(), request.HeadersDictionary, origin);
                                break;
                            default:
                                if (input.Length > 2000) _socket.SendBadRequest();
                                switch (input.Trim().ToUpperInvariant())
                                {
                                    case "CLOSE":
                                        _socket.Close();
                                        break;
                                    case "?":
                                        _socket.Send($">>> {query}\n");
                                        break;
                                    case "RELOAD":
                                        _socket.SendGET(query, request.HeadersDictionary, origin);
                                        break;
                                    case var other:
                                        _socket.SendUnknownCommand(other);
                                        break;
                                }
                                break;
                        }
                    });
                    return HandlerStatus.Handled;
                }));

            Handle.WebSocket(_Port, "restar_console", (s, socket) =>
            {
                if (socket.ToUInt64() != Console.ToUInt64())
                {
                    Console.Disconnect();
                    Console = socket;
                }
                switch (s.ToUpperInvariant().Trim())
                {
                    case "": break;
                    case "BEGIN":
                        ConsoleActive = true;
                        Console.Send(">>> Status: ACTIVE\n\n");
                        break;
                    case "PAUSE":
                        ConsoleActive = false;
                        Console.Send(">>> Status: PAUSED\n\n");
                        break;
                    case "CLOSE":
                        Console.Send(">>> Status: CLOSED\n\n");
                        Console.Disconnect();
                        Console = null;
                        break;
                    case var unrecognized:
                        Console.Send($">>> Unrecognized command: '{unrecognized}'\n");
                        break;
                }
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

        #region Websockets extensions

        private static void SendGETResult(this WebSocket ws, IFinalizedResult result)
        {
            if (result is ConsoleInit)
            {
                ws.Send(">>> 400: Bad request. Cannot enter the WebSocket console from another WebSocket.\n");
                return;
            }
            if (result.StatusCode == HttpStatusCode.NoContent)
                ws.SendNoContent();
            else
            {
                ws.Send(result.Body.ToByteArray());
                ws.Send("\n");
            }
        }

        private static void SendGET(this WebSocket ws, string query, Dictionary<string, string> headers, Origin origin)
        {
            var result = Evaluate(GET, query, null, headers, origin);
            ws.SendGETResult(result);
        }

        private static void SendPOST(this WebSocket ws, string query, byte[] body, Dictionary<string, string> headers, Origin origin)
        {
            var result = Evaluate(POST, query, body, headers, origin);
            ws.Send($">>> {result.StatusCode.ToCode()}: {result.StatusDescription}. " +
                    $"{result.Headers["RESTar-Info"]} {result.Headers["ErrorInfo"]}\n");
        }

        private static void SendNoContent(this WebSocket ws)
        {
            ws.Send("204: No content\n");
        }

        private static void SendBadRequest(this WebSocket ws)
        {
            ws.Send(">>> 400: Bad request\n");
        }

        private static void SendUnknownCommand(this WebSocket ws, string command)
        {
            ws.Send($">>> 400: Bad request. Unknown command '{command}'.\n");
        }

        private static void Close(this WebSocket ws)
        {
            ws.Send(">>> Now closing...\n");
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
                    "> To close, type CLOSE\n\n");
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