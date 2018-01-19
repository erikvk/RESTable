using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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

    [RESTar(Description = "The console")]
    internal class Console : ITerminal
    {
        public ConsoleStatus Status { get; set; }
        public bool ShowWelcomeText { get; set; } = true;
        public bool IncludeContent { get; set; } = false;

        private IWebSocketInternal WebSocketInternal;

        public void Open()
        {
            Consoles[this] = default;
            Status = PAUSED;
            if (ShowWelcomeText)
                SendConsoleInit();
        }

        public IWebSocket WebSocket
        {
            private get => WebSocketInternal;
            set => WebSocketInternal = (IWebSocketInternal) value;
        }

        public void Dispose() => Consoles.Remove(this);
        public void HandleBinaryInput(byte[] input) => throw new NotImplementedException();
        public bool SupportsTextInput { get; } = true;
        public bool SupportsBinaryInput { get; } = false;

        private static string Content(string content) => $" Content: {content}";
        private static string Content(byte[] content) => $" Content: {Encoding.UTF8.GetString(content)}";

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

        internal static void LogHTTPRequest(string requestId, Action action, string uri, IPAddress clientIpAddress)
        {
            if (uri.Length == 0) uri = "/";
            SendToAll($"=> [{requestId}] {action} '{uri}' from '{clientIpAddress}' at {_DateTime}");
        }

        internal static void LogHTTPResult(string requestId, IFinalizedResult result)
        {
            var info = result.Headers["RESTar-Info"];
            var errorInfo = result.Headers["ErrorInfo"];
            var tail = "";
            if (info != null)
                tail += $". {info}";
            if (errorInfo != null)
                tail += $". See {errorInfo}";
            SendToAll($"<= [{requestId}] {result.StatusCode.ToCode()}: '{result.StatusDescription}'. " +
                      $"{tail} at {_DateTime}");
        }

        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.ff";

        private static string _DateTime => DateTime.Now.ToString(DateTimeFormat);

        private static string TerminalResourcePart(IWebSocketInternal webSocket, string direction) =>
            webSocket.TerminalResource != null ? $"{direction} '{webSocket.TerminalResource.FullName}' " : null;

        private const string This = "RESTar.Admin.Console";
        private static readonly IDictionary<Console, byte> Consoles;
        static Console() => Consoles = new ConcurrentDictionary<Console, byte>();

        internal static void LogWebSocketOpen(IWebSocketInternal webSocket)
        {
            if (!Consoles.Any() || webSocket.TerminalResource?.FullName == This) return;
            SendToAll($"++ [WS {webSocket.Id}] opened {TerminalResourcePart(webSocket, "to")}from " +
                      $"'{webSocket.TcpConnection.ClientIP}' at {webSocket.Opened.ToString(DateTimeFormat)}");
        }

        internal static void LogWebSocketClosed(IWebSocketInternal webSocket)
        {
            if (!Consoles.Any() || webSocket.TerminalResource?.FullName == This) return;
            SendToAll($"-- [WS {webSocket.Id}] closed {TerminalResourcePart(webSocket, "to")}from " +
                      $"'{webSocket.TcpConnection.ClientIP}' at {webSocket.Closed.ToString(DateTimeFormat)}");
        }

        internal static void LogWebSocketTextInput(string input, IWebSocketInternal webSocket)
        {
            if (!Consoles.Any() || webSocket.TerminalResource?.FullName == This) return;
            var length = Encoding.UTF8.GetByteCount(input);
            SendToAll($"=> [WS {webSocket.Id}] received {length} bytes {TerminalResourcePart(webSocket, "to")}from " +
                      $"'{webSocket.TcpConnection.ClientIP}' at {_DateTime}.", input);
        }

        internal static void LogWebSocketBinaryInput(byte[] input, IWebSocketInternal webSocket)
        {
            if (!Consoles.Any() || webSocket.TerminalResource?.FullName == This) return;
            SendToAll($"=> [WS {webSocket.Id}] received {input.Length} bytes {TerminalResourcePart(webSocket, "to")}from " +
                      $"'{webSocket.TcpConnection.ClientIP}' at {_DateTime}.", input);
        }

        internal static void LogWebSocketTextOutput(string output, IWebSocketInternal webSocket)
        {
            if (!Consoles.Any() || webSocket.TerminalResource?.FullName == This) return;
            var length = Encoding.UTF8.GetByteCount(output);
            SendToAll($"<= [WS {webSocket.Id}] sent {length} bytes to '{webSocket.TcpConnection.ClientIP}' " +
                      $"{TerminalResourcePart(webSocket, "from")}at {_DateTime}.", output);
        }

        internal static void LogWebSocketBinaryOutput(byte[] output, IWebSocketInternal webSocket)
        {
            if (!Consoles.Any() || webSocket.TerminalResource?.FullName == This) return;
            SendToAll($"<= [WS {webSocket.Id}] sent {output.Length} bytes to '{webSocket.TcpConnection.ClientIP}' " +
                      $"{TerminalResourcePart(webSocket, "from")}at {_DateTime}.", output);
        }

        private static void SendToAll(string message, string stringContent) => Consoles.Keys
            .AsParallel()
            .Where(c => c.Status == ACTIVE)
            .ForEach(c =>
            {
                if (c.IncludeContent)
                    message = message + Content(stringContent);
                c.WebSocketInternal.SendTextRaw(message);
            });

        private static void SendToAll(string message, byte[] bytesContent) => Consoles.Keys
            .AsParallel()
            .Where(c => c.Status == ACTIVE)
            .ForEach(c =>
            {
                if (c.IncludeContent)
                    message = message + Content(bytesContent);
                c.WebSocketInternal.SendTextRaw(message);
            });

        private static void SendToAll(string message) => Consoles.Keys
            .AsParallel()
            .Where(c => c.Status == ACTIVE)
            .ForEach(c => c.WebSocketInternal.SendTextRaw(message));
    }
}