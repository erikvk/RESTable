using System;
using System.Net;
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

    internal class Console : ITerminal
    {
        internal ConsoleStatus Status;
        private IWebSocket _webSocket;

        public IWebSocket WebSocket
        {
            private get => _webSocket;
            set
            {
                _webSocket = value;
                SendConsoleInit();
            }
        }

        public string Description => "The console";
        public void HandleBinaryInput(byte[] input) => throw new NotImplementedException();
        public bool HandlesText => true;
        public bool HandlesBinary => false;

        private void SendConsoleInit() => WebSocket
            .SendText("### Welcome to the RESTar WebSocket console! ###\n\n" +
                      ">>> Status: PAUSED\n\n" +
                      "> To begin, type BEGIN\n" +
                      "> To pause, type PAUSE\n" +
                      "> To close, type CLOSE\n");

        public void HandleTextInput(string input)
        {
            switch (input.ToUpperInvariant().Trim())
            {
                case "": break;
                case "BEGIN":
                    Status = ACTIVE;
                    WebSocket.SendText("Status: ACTIVE\n");
                    break;
                case "PAUSE":
                    Status = PAUSED;
                    WebSocket.SendText("Status: PAUSED\n");
                    break;
                case "EXIT":
                case "QUIT":
                case "DISCONNECT":
                case "CLOSE":
                    WebSocket.SendText("Status: CLOSED\n");
                    WebSocket.Disconnect();
                    break;
                case var unrecognized:
                    WebSocket.SendText($"Unknown command '{unrecognized}'");
                    break;
            }
        }

        internal void LogHTTPRequest(string requestId, Action action, string uri, IPAddress clientIpAddress)
        {
            WebSocket.SendText($"=> [{requestId}] {action} '{uri}' from '{clientIpAddress}'  @ {DateTime.Now:O}");
        }

        internal void LogHTTPResult(string requestId, IFinalizedResult result)
        {
            var info = result.Headers["RESTar-Info"];
            var errorInfo = result.Headers["ErrorInfo"];
            var tail = "";
            if (info != null)
                tail += $". {info}";
            if (errorInfo != null)
                tail += $". See {errorInfo}";
            WebSocket.SendText($"<= [{requestId}] {result.StatusCode.ToCode()}: '{result.StatusDescription}'. " +
                               $"{tail}  @ {DateTime.Now:O}");
        }

        internal void LogWebSocketInput(string input, IWebSocketInternal webSocket)
        {
            if (webSocket.Equals(WebSocket)) return;
            WebSocket.SendText($"=> [WS {webSocket.Id}] Received: '{input}' at '{webSocket.CurrentLocation}' from " +
                               $"'{webSocket.ClientIpAddress}'  @ {DateTime.Now:O}");
        }

        internal void LogWebSocketOutput(string output, IWebSocketInternal webSocket)
        {
            if (webSocket.Equals(WebSocket)) return;
            WebSocket.SendText($"<= [WS {webSocket.Id}] Sent: '{output}'  @ {DateTime.Now:O}");
        }
    }
}