using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using RESTar.Operations;
using static RESTar.Admin.ConsoleStatus;

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
        public IEnumerable<Console> Select(IRequest<Console> request) => throw new NotImplementedException();
        public long Count(IRequest<Console> request) => 0;
        
        static Console() => Sockets = new ConcurrentDictionary<IWebSocket, ConsoleStatus>();
        private static readonly IDictionary<IWebSocket, ConsoleStatus> Sockets;

        public void HandleWebSocketConnection(IWebSocket webSocket)
        {
            Sockets[webSocket] = 0;
            webSocket.InputHandler = ConsoleShell;
            webSocket.DisconnectHandler = ws => Sockets.Remove(webSocket);
        }

        private static void ConsoleShell(IWebSocket ws, string input)
        {
            switch (input.ToUpperInvariant().Trim())
            {
                case "": break;
                case "BEGIN":
                    Sockets[ws] = ACTIVE;
                    ws.Send("Status: ACTIVE\n");
                    break;
                case "PAUSE":
                    Sockets[ws] = PAUSED;
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


        internal static void LogRequest(ulong requestId, Requests.Action action, string uri, IPAddress clientIpAddress)
        {
            foreach (var pair in Sockets.AsParallel().Where(p => p.Value == ACTIVE))
                pair.Key.Send($"=> [{requestId}] {action} '{uri}' from '{clientIpAddress}'");
        }

        internal static void LogResult(ulong requestId, IFinalizedResult result)
        {
            foreach (var pair in Sockets.AsParallel().Where(p => p.Value == ACTIVE))
                pair.Key.Send($"<= [{requestId}] {result.StatusCode.ToCode()}: '{result.StatusDescription}'. " +
                              $"{result.Headers["RESTar-info"]} {result.Headers["ErrorInfo"]}");
        }
    }
}