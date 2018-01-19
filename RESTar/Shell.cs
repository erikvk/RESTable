using System;
using System.Linq;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Results.Fail.BadRequest;
using RESTar.Results.Success;
using static RESTar.Internal.ErrorCodes;
using Action = RESTar.Requests.Action;

namespace RESTar
{
    [RESTar(Description = "The shell")]
    internal class Shell : ITerminal
    {
        private string query = "";

        public string Query
        {
            get => query;
            set
            {
                switch (value)
                {
                    case "": break;
                    case null:
                    case var _ when value[0] != '/': throw new InvalidSyntax(InvalidUriSyntax, "Shell queries must begin with '/'");
                }
                query = value;
            }
        }

        public bool Silent { get; set; }
        public bool Unsafe { get; set; }

        private Func<IUriParameters> GetNextPageLink;
        private System.Action OnConfirm;
        private IEntitiesMetadata PreviousResultMetadata;

        public IWebSocket WebSocket { get; set; }
        public void HandleBinaryInput(byte[] input) => throw new NotImplementedException();
        public bool SupportsTextInput { get; } = true;
        public bool SupportsBinaryInput { get; } = false;
        internal static TerminalResource TerminalResource { get; set; }

        public void Open()
        {
            WebSocket.TcpConnection.Origin = OriginType.Shell;
            if (Query != "")
                SafeOperation(Action.GET);
            else SendShellInit();
        }

        public void HandleTextInput(string input)
        {
            if (OnConfirm != null)
            {
                switch (input.FirstOrDefault())
                {
                    case var _ when input.Length > 1:
                    default:
                        SendConfirmationRequest();
                        break;
                    case 'Y':
                    case 'y':
                        OnConfirm();
                        OnConfirm = null;
                        break;
                    case 'N':
                    case 'n':
                        OnConfirm = null;
                        SendCancel();
                        break;
                }
                return;
            }
            switch (input.FirstOrDefault())
            {
                case '\0':
                case '\n': break;
                case ' ' when input.Length == 1:
                    SafeOperation(Action.GET);
                    break;
                case '-':
                case '/':
                    Query = input.Trim();
                    SafeOperation(Action.GET);
                    break;
                case '[':
                case '{':
                    SafeOperation(Action.POST, input.ToBytes());
                    break;
                case var _ when input.Length > 2000:
                    SendBadRequest();
                    break;
                default:
                    var (command, tail) = input.Trim().TSplit(' ');
                    switch (command.ToUpperInvariant())
                    {
                        case "GET":
                            if (!string.IsNullOrWhiteSpace(tail))
                                Query = tail;
                            SafeOperation(Action.GET);
                            break;
                        case "POST":
                            SafeOperation(Action.POST, tail.ToBytes());
                            break;
                        case "PUT":
                            SendBadRequest("PUT is not available in the WebSocket interface");
                            break;
                        case "PATCH":
                            UnsafeOperation(Action.PATCH, tail.ToBytes());
                            break;
                        case "DELETE":
                            UnsafeOperation(Action.DELETE);
                            break;
                        case "REPORT":
                            if (!string.IsNullOrWhiteSpace(tail))
                                Query = tail;
                            SafeOperation(Action.REPORT);
                            break;
                        case "HELP":
                            SendHelp();
                            break;
                        case "EXIT":
                        case "QUIT":
                        case "DISCONNECT":
                        case "CLOSE":
                            Close();
                            break;
                        case "?":
                            WebSocket.SendText($"{(Query.Any() ? Query : "/")}");
                            break;
                        case "RELOAD":
                            SafeOperation(Action.GET);
                            break;
                        case "NEXT":
                            var link = GetNextPageLink?.Invoke()?.ToString();
                            if (link == null)
                                SendResult(new NoContent());
                            else
                            {
                                Query = link;
                                SafeOperation(Action.GET);
                            }
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

                            WebSocket.SendText(getGreeting());
                            break;
                        case "NICE":
                        case "THANKS":
                        case "THANK":

                            string getYoureWelcome()
                            {
                                switch (new Random().Next(0, 7))
                                {
                                    case 0: return "😎";
                                    case 1: return "👍";
                                    case 2: return "🙌";
                                    case 3: return "🎉";
                                    case 4: return "🤘";
                                    case 5: return "You're welcome!";
                                    default: return "✌️";
                                }
                            }

                            WebSocket.SendText(getYoureWelcome());
                            break;
                        case "CREDITS":
                            SendCredits();
                            break;
                        case var unknown:
                            SendUnknownCommand(unknown);
                            break;
                    }
                    break;
            }
        }

        public void Dispose()
        {
            WebSocket.TcpConnection.Origin = OriginType.External;
            OnConfirm = null;
            PreviousResultMetadata = null;
            GetNextPageLink = null;
            query = "";
        }

        private IFinalizedResult WsEvaluate(Action action, byte[] body)
        {
            var result = RequestEvaluator.Evaluate(action, ref query, body, WebSocket.Headers, WebSocket.TcpConnection);
            if (result is IEntitiesMetadata entitiesMetaData)
            {
                PreviousResultMetadata = entitiesMetaData;
                GetNextPageLink = entitiesMetaData.GetNextPageLink;
            }
            return result;
        }

        private void SafeOperation(Action action, byte[] body = null) => SendResult(WsEvaluate(action, body));

        private void UnsafeOperation(Action action, byte[] body = null)
        {
            void operate()
            {
                WebSocket.Headers.UnsafeOverride = true;
                SendResult(WsEvaluate(action, body));
            }

            switch (PreviousResultMetadata?.EntityCount)
            {
                case null:
                case 0:
                    SendBadRequest($". No entities for {action} operation. Make a selecting request before running {action}");
                    break;
                case 1:
                    operate();
                    break;
                case var many:
                    if (Unsafe)
                    {
                        operate();
                        break;
                    }
                    OnConfirm = operate;
                    SendConfirmationRequest($"This will run {action} on {many} entities in resource '{PreviousResultMetadata.ResourceFullName}'. ");
                    break;
            }
        }

        private void SendResult(IFinalizedResult result) => WebSocket.SendResult(result);

        private void SendShellInit()
        {
            if (Silent) return;
            WebSocket.SendText("### Entering the RESTar WebSocket shell... ###");
            WebSocket.SendText("### Type a command to continue (e.g. HELP) ###");
        }

        private void SendConfirmationRequest(string initialInfo = null) => WebSocket.SendText($"{initialInfo}Type 'Y' to continue, 'N' to cancel");
        private void SendCancel() => WebSocket.SendText("Operation cancelled");
        private void SendBadRequest(string message = null) => WebSocket.SendText($"400: Bad request{message}");
        private void SendInvalidCommandArgument(string command, string arg) => WebSocket.SendText($"Invalid argument '{arg}' for command '{command}'");
        private void SendUnknownCommand(string command) => WebSocket.SendText($"Unknown command '{command}'");

        private void Close()
        {
            if (Silent) return;
            WebSocket.SendText("### Closing the RESTar WebSocket shell... ###");
            WebSocket.Disconnect();
        }

        private void SendHelp() => WebSocket.SendText(
            "\n  The RESTar WebSocket shell makes it possible to send\n" +
            "  multiple requests to a RESTar API, over a single TCP\n" +
            "  connection. Using commands, the client can navigate\n" +
            "  around the resources of the API, read, insert, update\n" +
            "  and/or delete entities, or enter terminals. To navigate\n" +
            "  and select entities, simply send a request URI over this\n" +
            "  WebSocket, e.g. '/availableresource//limit=3'. To insert\n" +
            "  an entity into a resource, send the JSON representation\n" +
            "  over the WebSocket. To update entities, send 'PATCH <json>',\n" +
            "  where <json> is the JSON data to update entities from. To\n" +
            "  delete selected entities, send 'DELETE'. For potentially\n" +
            "  unsafe operations, you will be asked to confirm before\n" +
            "  changes are applied.\n\n" +
            "  Some other simple commands:\n" +
            "  ?           Prints the current location\n" +
            "  REPORT      Counts the entities at the current location\n" +
            "  RELOAD      Relods the current location\n" +
            "  HELP        Prints this help page\n" +
            "  CLOSE       Closes the WebSocket\n");

        private void SendCredits()
        {
            WebSocket.SendText($"RESTar is designed and developed by Erik von Krusenstierna, © Mopedo AB {DateTime.Now.Year}");
        }
    }
}