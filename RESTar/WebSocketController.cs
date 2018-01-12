using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Results.Fail.Forbidden;
using RESTar.Results.Success;
using static RESTar.Requests.Action;
using Action = RESTar.Requests.Action;

namespace RESTar
{
    internal static class WebSocketController
    {
        private static readonly IDictionary<Type, ConcurrentBag<IWebSocket>> ActiveSockets;
        private static readonly IDictionary<IWebSocket, IEntitiesMetadata> PreviousResultMetadata;
        private static readonly IDictionary<IWebSocket, System.Action> OnConfirmationActions;
        private static readonly IDictionary<string, IWebSocket> AllSockets;

        internal static IWebSocket Get(string wsId) => AllSockets.SafeGet(wsId);

        static WebSocketController()
        {
            var comparer = new WebSocketComparer();
            ActiveSockets = new ConcurrentDictionary<Type, ConcurrentBag<IWebSocket>>();
            PreviousResultMetadata = new ConcurrentDictionary<IWebSocket, IEntitiesMetadata>(comparer);
            OnConfirmationActions = new ConcurrentDictionary<IWebSocket, System.Action>(comparer);
            AllSockets = new ConcurrentDictionary<string, IWebSocket>();
        }



        internal static void HandleDisconnect(string wsId)
        {
            if (!AllSockets.TryGetValue(wsId, out var ws)) return;
            OnConfirmationActions.Remove(ws);
            PreviousResultMetadata.Remove(ws);
            AllSockets.Remove(wsId);
        }

        #region Shell

        internal static void Shell(string input, IWebSocket ws, ref string query, Headers headers, TCPConnection tcpConnection)
        {
            if (OnConfirmationActions.TryGetValue(ws, out var action))
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
                        OnConfirmationActions.Remove(ws);
                        break;
                    case 'N':
                    case 'n':
                        OnConfirmationActions.Remove(ws);
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
                    ws.SafeOperation(GET, ref query, null, headers, tcpConnection);
                    break;
                case '-':
                case '/':
                    query = input.Trim();
                    ws.SafeOperation(GET, ref query, null, headers, tcpConnection);
                    break;
                case '[':
                case '{':
                    ws.SafeOperation(POST, ref query, input.ToBytes(), headers, tcpConnection);
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
                            ws.SafeOperation(GET, ref query, null, headers, tcpConnection);
                            break;
                        case "POST":
                            ws.SafeOperation(POST, ref query, tail.ToBytes(), headers, tcpConnection);
                            break;
                        case "PUT":
                            ws.SendBadRequest("PUT is not available in the WebSocket interface");
                            break;
                        case "PATCH":
                            ws.UnsafeOperation(PATCH, query, tail.ToBytes(), headers, tcpConnection);
                            break;
                        case "DELETE":
                            ws.UnsafeOperation(DELETE, query, null, headers, tcpConnection);
                            break;
                        case "REPORT":
                            if (!string.IsNullOrWhiteSpace(tail))
                                query = tail;
                            ws.SafeOperation(REPORT, ref query, null, headers, tcpConnection);
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
                            ws.SafeOperation(GET, ref query, null, headers, tcpConnection);
                            break;
                        case "HI":
                        case "HELLO":

                            string getGreeting()
                            {
                                switch (new Random().Next(0, 10))
                                {
                                    case 0: return "Well, hello there :D";
                                    case 1: return "Greetings, friend";
                                    case 2: return "Hello, dear client";
                                    case 3: return "Hello to you";
                                    case 4: return "Hi!";
                                    case 5: return "Nice to see you!";
                                    case 6: return "What's up?";
                                    case 7: return "✌️";
                                    case 8: return "'sup";
                                    default: return "Oh no, it's you again...";
                                }
                            }

                            ws.Send(getGreeting());
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

        private static IFinalizedResult WsEvaluate(this IWebSocket ws, Action action, ref string query, byte[] body, Headers headers,
            TCPConnection tcpConnection)
        {
            var result = RequestEvaluator.Evaluate(action, ref query, body, headers, tcpConnection);
            if (result is IEntitiesMetadata entitiesMetaData)
                PreviousResultMetadata[ws] = entitiesMetaData;
            return result;
        }

        private static void SafeOperation(this IWebSocket ws, Action action, ref string query, byte[] body, Headers headers,
            TCPConnection tcpConnection)
        {
            ws.SendResult(ws.WsEvaluate(action, ref query, body, headers, tcpConnection));
        }

        private static void UnsafeOperation(this IWebSocket ws, Action action, string query, byte[] body, Headers headers, TCPConnection tcpConnection)
        {
            void operate()
            {
                headers.UnsafeOverride = true;
                var result = ws.WsEvaluate(action, ref query, body, headers, tcpConnection);
                ws.SendStatus(result);
            }

            var entitiesMetaData = PreviousResultMetadata.SafeGet(ws);
            switch (entitiesMetaData?.EntityCount)
            {
                case null:
                case 0:
                    ws.SendBadRequest($". No entities for {action} operation. Make a selecting request before running {action}");
                    break;
                case 1:
                    operate();
                    break;
                case var many:
                    OnConfirmationActions[ws] = operate;
                    ws.SendConfirmationRequest($"This will run {action} on {many} entities in resource '{entitiesMetaData.ResourceFullName}'. ");
                    break;
            }
        }

        internal static void SendInitialResult(IWebSocket ws, IFinalizedResult result)
        {
            switch (result)
            {
                case IEntitiesMetadata entitiesMetadata:
                    PreviousResultMetadata[ws] = entitiesMetadata;
                    break;
                case NoContent _: return;
            }
            ws.SendResult(result);
        }

        private static void SendResult(this IWebSocket ws, IFinalizedResult result)
        {
            switch (result)
            {
                case ConsoleInit _:
                    ws.Send("400: Bad request. Cannot enter the WebSocket console from another WebSocket");
                    break;
                case Report _:
                case Entities _:
                    ws.Send(result.Body);
                    break;
                default:
                    ws.SendStatus(result);
                    break;
            }
        }

        private static void SendStatus(this IWebSocket ws, IFinalizedResult result)
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

        private static void SendConfirmationRequest(this IWebSocket ws, string initialInfo = null)
        {
            ws.Send($"{initialInfo}Type 'Y' to continue, 'N' to cancel");
        }

        private static void SendCancel(this IWebSocket ws)
        {
            ws.Send("Operation cancelled");
        }

        private static void SendBadRequest(this IWebSocket ws, string message = null)
        {
            ws.Send($"400: Bad request{message}");
        }

        private static void SendUnknownCommand(this IWebSocket ws, string command)
        {
            ws.Send($"Unknown command '{command}'");
        }

        private static void Close(this IWebSocket ws)
        {
            ws.Send("Closing RESTar WebSocket interface...");
            ws.Disconnect();
        }

        private static void SendHelp(this IWebSocket ws)
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

        private static void SendCredits(this IWebSocket ws)
        {
            ws.Send("RESTar is designed and developed by Erik von Krusenstierna");
        }

        private static void SendConsoleInit(this IWebSocket ws)
        {
            ws.Send("### Welcome to the RESTar WebSocket console! ###\n\n" +
                    ">>> Status: PAUSED\n\n" +
                    "> To begin, type BEGIN\n" +
                    "> To pause, type PAUSE\n" +
                    "> To close, type CLOSE\n");
        }

        #endregion
    }
}