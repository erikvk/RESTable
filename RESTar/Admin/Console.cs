using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using RESTar.Operations;
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
    internal class Console : ISelector<Console>, ICounter<Console>, IWebSocketController
    {
        private const string ThisTarget = nameof(RESTar) + "." + nameof(Admin) + "." + nameof(Console);

        public IEnumerable<Console> Select(IRequest<Console> request) => null;
        public long Count(IRequest<Console> request) => 0;

        static Console() => ActiveConsoles = new ConcurrentDictionary<IWebSocket, ConsoleStatus>();
        private static readonly IDictionary<IWebSocket, ConsoleStatus> ActiveConsoles;

        public void HandleWebSocketConnection(IWebSocket webSocket)
        {
            ActiveConsoles[webSocket] = 0;
            webSocket.InputHandler = ConsoleShell;
            webSocket.DisconnectHandler = ws => ActiveConsoles.Remove(webSocket);
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
                    ActiveConsoles[ws] = ACTIVE;
                    ws.Send("Status: ACTIVE\n");
                    break;
                case "PAUSE":
                    ActiveConsoles[ws] = PAUSED;
                    ws.Send("Status: PAUSED\n");
                    break;
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
            if (!ActiveConsoles.Any()) return;
            SendToAll($"=> [{requestId}] {action} '{uri}' from '{clientIpAddress}'  @ {DateTime.Now:O}");
        }

        internal static void LogResult(string requestId, IFinalizedResult result)
        {
            if (!ActiveConsoles.Any()) return;
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

        internal static void LogWebSocketInput(string input, IWebSocketInternal webSocket)
        {
            if (!ActiveConsoles.Any() || webSocket.Target.FullName == ThisTarget) return;
            SendToAll($"=> [WS {webSocket.Id}] Received: '{input}' at '{webSocket.CurrentLocation}' from " +
                      $"'{webSocket.ClientIpAddress}'  @ {DateTime.Now:O}");
        }

        internal static void LogWebSocketOutput(IWebSocketInternal webSocket, string output)
        {
            if (!ActiveConsoles.Any() || webSocket.Target.FullName == ThisTarget) return;
            SendToAll($"<= [WS {webSocket.Id}] Sent: '{output}'  @ {DateTime.Now:O}");
        }

        private static void SendToAll(string message)
        {
            foreach (var pair in ActiveConsoles.AsParallel().Where(p => p.Value == ACTIVE))
                pair.Key.Send(message);
        }
    }
}