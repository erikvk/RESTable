using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.WebSockets;
using static RESTar.Admin.ConsoleStatus;
using Action = RESTar.Requests.Action;

namespace RESTar.Admin
{
    internal enum ConsoleStatus : byte
    {
        PAUSED,
        ACTIVE
    }

    [RESTar(Methods.GET)]
    internal class Console : ISelector<Console>, ICounter<Console>
    {
        public IEnumerable<Console> Select(IRequest<Console> request) => null;
        public long Count(IRequest<Console> request) => 0;
        static Console() => Consoles = new ConcurrentDictionary<IWebSocket, ConsoleStatus>();
        private static readonly IDictionary<IWebSocket, ConsoleStatus> Consoles;

        public void HandleWebSocketConnection(IWebSocket webSocket)
        {
            Consoles[webSocket] = 0;
            webSocket.InputHandler = ConsoleShell;
            webSocket.DisconnectHandler = ws => Consoles.Remove(webSocket);
            SendConsoleInit(webSocket);
        }

        private static void SendConsoleInit(IWebSocket ws)
        {
            ws.Send("### Welcome to the RESTar WebSocket console! ###\n\n" +
                    ">>> Status: PAUSED\n\n" +
                    "> To begin, type BEGIN\n" +
                    "> To pause, type PAUSE\n" +
                    "> To close, type CLOSE\n");
        }

        private static void ConsoleShell(IWebSocket ws, string input)
        {
            switch (input.ToUpperInvariant().Trim())
            {
                case "": break;
                case "BEGIN":
                    Consoles[ws] = ACTIVE;
                    ws.Send("Status: ACTIVE\n");
                    break;
                case "PAUSE":
                    Consoles[ws] = PAUSED;
                    ws.Send("Status: PAUSED\n");
                    break;
                case "EXIT":
                case "QUIT":
                case "DISCONNECT":
                case "CLOSE":
                    ws.Send("Status: CLOSED\n");
                    ws.Disconnect();
                    break;
                case var unrecognized:
                    ws.Send($"Unknown command '{unrecognized}'");
                    break;
            }
        }

        internal static void LogRequest(string requestId, Action action, string uri, IPAddress clientIpAddress)
        {
            if (!Consoles.Any()) return;
            SendToAll($"=> [{requestId}] {action} '{uri}' from '{clientIpAddress}'  @ {DateTime.Now:O}");
        }

        internal static void LogResult(string requestId, IFinalizedResult result)
        {
            if (!Consoles.Any()) return;
            var info = result.Headers["RESTar-Info"];
            var errorInfo = result.Headers["ErrorInfo"];
            var tail = "";
            if (info != null)
                tail += $". {info}";
            if (errorInfo != null)
                tail += $". See {errorInfo}";
            SendToAll($"<= [{requestId}] {result.StatusCode.ToCode()}: '{result.StatusDescription}'. " +
                      $"{tail}  @ {DateTime.Now:O}");
        }

        private const string ThisType = "RESTar.Admin.Console";

        internal static void LogWebSocketInput(string input, IWebSocketInternal webSocket)
        {
            if (!Consoles.Any() || webSocket.Target.FullName == ThisType) return;
            SendToAll($"=> [WS {webSocket.Id}] Received: '{input}' at '{webSocket.CurrentLocation}' from " +
                      $"'{webSocket.ClientIpAddress}'  @ {DateTime.Now:O}");
        }

        internal static void LogWebSocketOutput(IWebSocketInternal webSocket, string output)
        {
            if (!Consoles.Any() || webSocket.Target.FullName == ThisType) return;
            SendToAll($"<= [WS {webSocket.Id}] Sent: '{output}'  @ {DateTime.Now:O}");
        }

        private static void SendToAll(string message) => Consoles
            .AsParallel()
            .Where(p => p.Value == ACTIVE)
            .ForEach(p => p.Key.Send(message));
    }
}