using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Results.Error;
using RESTar.Results.Error.BadRequest;
using RESTar.Results.Success;
using RESTar.WebSockets;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Method;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// The WebSocket shell, used to navigate and execute commands against RESTar resources
    /// from a connected WebSocket. 
    /// </summary>
    [RESTar(Description = description, GETAvailableToAll = true)]
    public class Shell : ITerminal
    {
        private const string description = "The RESTar WebSocket shell lets the client navigate around the resources of the " +
                                           "RESTar application, perform CRUD operations and enter terminal resources.";

        private string query = "";
        private string previousQuery = "";
        private const long MaxStreamBufferSize = 16_000_000;
        private long _streamBufferSize = 16_000_000;
        private Func<int, IUriComponents> GetNextPageLink;
        private Action OnConfirm;
        private IEntities PreviousResultMetadata;
        private StreamManifest CurrentStreamManifest;


        /// <summary>
        /// Signals that there are changes to the query that have been made pre evaluation
        /// </summary>
        private bool queryChangedPreEval;

        internal static ITerminalResource<Shell> TerminalResource { get; set; }


        /// <summary>
        /// The query to perform in the shell
        /// </summary>
        public string Query
        {
            get => query;
            set
            {
                switch (value)
                {
                    case "": break;
                    case null:
                    case var _ when value[0] != '/' && value[0] != '-':
                        throw new InvalidSyntax(InvalidUriSyntax, "Shell queries must begin with '/' or '-'");
                }
                previousQuery = query;
                queryChangedPreEval = true;
                query = value;
            }
        }

        /// <summary>
        /// Should the shell be silent in output? Sets WriteStatusBeforeContent, WriteTimeElapsed, WriteQueryAfterContent
        /// and WriteInfoTexts to false.
        /// </summary>
        public bool Silent
        {
            get => !WriteStatusBeforeContent && !WriteTimeElapsed && !WriteQueryAfterContent && !WriteInfoTexts;
            set => WriteStatusBeforeContent = WriteTimeElapsed = WriteQueryAfterContent = WriteInfoTexts = !value;
        }

        /// <summary>
        /// Should unsafe commands be allowed?
        /// </summary>
        public bool Unsafe { get; set; } = false;

        /// <summary>
        /// Should the shell output the result status before included content?
        /// </summary>
        public bool WriteStatusBeforeContent { get; set; } = true;

        /// <summary>
        /// Should the shell output the time elapsed in evaluating the command?
        /// </summary>
        public bool WriteTimeElapsed { get; set; } = true;

        /// <summary>
        /// Should the shell output the current query after writing content?
        /// </summary>
        public bool WriteQueryAfterContent { get; set; } = true;

        /// <summary>
        /// The size of stream messages in bytes
        /// </summary>
        public long StreamBufferSize
        {
            get => _streamBufferSize;
            set
            {
                if (value < 512 || MaxStreamBufferSize < value)
                    _streamBufferSize = MaxStreamBufferSize;
                else _streamBufferSize = value;
                if (CurrentStreamManifest != null)
                    SetupStreamManifest();
            }
        }

        /// <summary>
        /// Should the shell output info texts?
        /// </summary>
        public bool WriteInfoTexts { get; set; } = true;


        /// <inheritdoc />
        public IWebSocket WebSocket { private get; set; }

        /// <inheritdoc />
        public void HandleBinaryInput(byte[] input)
        {
            if (!(input?.Length > 0)) return;
            if (Query.Length == 0 || OnConfirm != null)
                WebSocket.SendResult(new InvalidShellStateForBinaryInput(WebSocket));
            else SafeOperation(POST, input);
        }

        /// <inheritdoc />
        public bool SupportsTextInput { get; } = true;

        /// <inheritdoc />
        public bool SupportsBinaryInput { get; } = true;

        /// <inheritdoc />
        public void Open()
        {
            if (Query != "")
                SafeOperation(GET);
            else SendShellInit();
        }

        /// <inheritdoc />
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
                        CurrentStreamManifest.Dispose();
                        CurrentStreamManifest = null;
                        SendCancel();
                        break;
                }
                return;
            }
            if (CurrentStreamManifest != null)
            {
                var (command, arg) = input.TSplit(' ');
                switch (command.ToUpperInvariant())
                {
                    case "MANIFEST":
                        WebSocket.SendJson(CurrentStreamManifest);
                        break;
                    case "GET":
                        StreamMessage(CurrentStreamManifest.MessagesRemaining);
                        break;
                    case "NEXT" when int.TryParse(arg, out var nr):
                        StreamMessage(nr);
                        break;
                    case "NEXT":
                        StreamMessage(1);
                        break;
                    case "CLOSE":
                        CurrentStreamManifest.Dispose();
                        WebSocket.SendText($"499: Client closed request. Streamed {CurrentStreamManifest.CurrentMessageIndex} " +
                                           $"of {CurrentStreamManifest.NrOfMessages} messages.");
                        CurrentStreamManifest = null;
                        break;
                }
                return;
            }
            if (input == " ")
            {
                SafeOperation(GET);
                return;
            }
            switch (input.FirstOrDefault())
            {
                case '\0':
                case '\n': break;
                case '-':
                case '/':
                    if (input.Length == 1)
                        input = "/restar.availableresource";
                    if (input.StartsWith("//"))
                        input = $"/restar.availableresource/{input.Substring(2)}";
                    Query = input;
                    ValidateQuery();
                    break;
                case '[':
                case '{':
                    SafeOperation(POST, input.ToBytes());
                    break;
                case var _ when input.Length > 16_000_000:
                    SendBadRequest();
                    break;
                default:
                    var (command, tail) = input.TSplit(' ');
                    switch (command.ToUpperInvariant())
                    {
                        case "GET":
                            SafeOperation(GET, tail?.ToBytes());
                            break;
                        case "POST":
                            SafeOperation(POST, tail?.ToBytes());
                            break;
                        case "PATCH":
                            UnsafeOperation(PATCH, tail?.ToBytes());
                            break;
                        case "PUT":
                            SafeOperation(PUT, tail?.ToBytes());
                            break;
                        case "DELETE":
                            UnsafeOperation(DELETE, tail?.ToBytes());
                            break;
                        case "REPORT":
                            SafeOperation(REPORT, tail?.ToBytes());
                            break;
                        case "HEAD":
                            SafeOperation(HEAD, tail?.ToBytes());
                            break;
                        case "STREAM":
                            var result = WsEvaluate(GET, null);
                            if (result is Content content)
                            {
                                CurrentStreamManifest = new StreamManifest(content);
                                SetupStreamManifest();
                            }
                            else SendResult(result);
                            break;

                        case "HEADERS":
                        case "HEADER":
                            tail = tail?.Trim();
                            if (string.IsNullOrWhiteSpace(tail))
                            {
                                SendHeaders();
                                break;
                            }
                            var (key, value) = tail.TSplit('=');
                            key = key.Trim();
                            value = value?.Trim();
                            if (value == null)
                            {
                                SendHeaders();
                                break;
                            }
                            if (Headers.IsCustom(key))
                            {
                                if (value == "null")
                                {
                                    WebSocket.Headers.Remove(key);
                                    SendHeaders();
                                    break;
                                }
                                WebSocket.Headers[key] = value;
                                SendHeaders();
                            }
                            else WebSocket.SendText($"400: Bad request. Cannot read or write reserved header '{key}'.");
                            return;
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
                            WebSocket.SendText($"{(Query.Any() ? Query : "< empty >")}");
                            break;
                        case "NEXT":
                            var stopwatch = Stopwatch.StartNew();
                            if (tail == null || !int.TryParse(tail, out var count))
                                count = -1;
                            var link = GetNextPageLink?.Invoke(count)?.ToString();
                            if (link == null)
                                SendResult(new NoContent(WebSocket, stopwatch.Elapsed));
                            else
                            {
                                Query = link;
                                SafeOperation(GET);
                            }
                            break;

                        #region Nonsense

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

                        #endregion
                    }
                    break;
            }
        }

        private void StreamMessage(int nr)
        {
            try
            {
                var startIndex = CurrentStreamManifest.CurrentMessageIndex;
                var endIndex = startIndex + nr;
                if (endIndex > CurrentStreamManifest.NrOfMessages - 1)
                    endIndex = CurrentStreamManifest.NrOfMessages - 1;
                var buffer = new byte[StreamBufferSize];
                for (var i = startIndex; i <= endIndex; i += 1)
                {
                    CurrentStreamManifest.CurrentMessageIndex = i;
                    var message = CurrentStreamManifest.Messages[i];
                    CurrentStreamManifest.Content.Body.Read(buffer, 0, buffer.Length);
                    WebSocket.SendBinary(buffer, 0, (int) message.Length);
                    message.Sent = true;
                    CurrentStreamManifest.MessagesStreamed += 1;
                    CurrentStreamManifest.MessagesRemaining -= 1;
                    CurrentStreamManifest.BytesStreamed += message.Length;
                    CurrentStreamManifest.BytesRemaining -= message.Length;
                }
                if (endIndex == CurrentStreamManifest.NrOfMessages - 1 && WriteInfoTexts)
                {
                    WebSocket.SendText($"200: OK. Sucessfully streamed {CurrentStreamManifest.NrOfMessages} messages.");
                    CurrentStreamManifest.Dispose();
                    CurrentStreamManifest = null;
                }
            }
            catch (Exception e)
            {
                WebSocket.SendException(e);
                WebSocket.SendText("500: Error during streaming. Streamed " + $"{CurrentStreamManifest?.CurrentMessageIndex ?? 0} " +
                                   $"of {CurrentStreamManifest?.NrOfMessages ?? 1} messages.");
                CurrentStreamManifest?.Dispose();
                CurrentStreamManifest = null;
            }
        }

        private void SendHeaders() => WebSocket.SendJson(new {WebSocket.Headers});

        /// <inheritdoc />
        public void Dispose()
        {
            OnConfirm = null;
            PreviousResultMetadata = null;
            GetNextPageLink = null;
            query = "";
            previousQuery = "";
            CurrentStreamManifest?.Dispose();
            WriteInfoTexts = true;
            WriteStatusBeforeContent = true;
            WriteTimeElapsed = true;
            WriteQueryAfterContent = true;
        }

        private ISerializedResult WsEvaluate(Method method, byte[] body)
        {
            if (Query.Length == 0) return new NoQuery(WebSocket, default);
            var local = Query;
            var result = Request.Create(WebSocket, method, ref local, body, WebSocket.Headers).Result.Serialize();
            switch (result)
            {
                case RESTarError _ when queryChangedPreEval:
                    query = previousQuery;
                    break;
                case IEntities entities:
                    query = local;
                    PreviousResultMetadata = entities;
                    GetNextPageLink = entities.GetNextPageLink;
                    break;
                default:
                    query = local;
                    break;
            }
            queryChangedPreEval = false;
            return result;
        }

        private void ValidateQuery()
        {
            var localQuery = Query;
            if (!Request.IsValid(WebSocket, ref localQuery, out var error, out var resource))
            {
                query = previousQuery;
                SendResult(error);
            }
            else
            {
                query = localQuery;
                if (resource is ITerminalResource)
                    SafeOperation(GET);
                else WebSocket.SendText("? " + Query);
            }
            queryChangedPreEval = false;
        }

        private void SetupStreamManifest()
        {
            var data = CurrentStreamManifest.Content;
            var dataLength = data.Body.Length;
            if (dataLength == 0) return;
            var nrOfMessages = dataLength / StreamBufferSize;
            var last = dataLength % StreamBufferSize;
            if (last > 0) nrOfMessages += 1;
            else last = StreamBufferSize;
            var messages = new StreamManifestMessage[nrOfMessages];
            long startIndex = 0;
            for (var i = 0; i < messages.Length; i += 1)
            {
                messages[i] = new StreamManifestMessage
                {
                    StartIndex = startIndex,
                    Length = StreamBufferSize
                };
                startIndex += StreamBufferSize;
            }
            messages.Last().Length = last;
            CurrentStreamManifest.NrOfMessages = (int) nrOfMessages;
            CurrentStreamManifest.MessagesRemaining = (int) nrOfMessages;
            CurrentStreamManifest.Messages = messages;
            WebSocket.SendJson(CurrentStreamManifest);
        }

        private void SafeOperation(Method method, byte[] body = null)
        {
            var sw = Stopwatch.StartNew();
            switch (WsEvaluate(method, body))
            {
                case Content tooLarge when tooLarge.Body is FileStream || tooLarge.Body.Length > 16_000_000:
                    CurrentStreamManifest = new StreamManifest(tooLarge);
                    OnConfirm = SetupStreamManifest;
                    SendConfirmationRequest("426: The response message is too large. Do you wish to stream the response? ");
                    break;
                case Content content:
                    SendResult(content, sw.Elapsed);
                    break;
                case var other:
                    SendResult(other);
                    break;
            }
            sw.Stop();
        }

        private void UnsafeOperation(Method method, byte[] body = null)
        {
            void operate()
            {
                WebSocket.Headers.UnsafeOverride = true;
                SafeOperation(method, body);
            }

            switch (PreviousResultMetadata?.EntityCount)
            {
                case null:
                case 0:
                    SendBadRequest($". No entities for {method} operation. Make a selecting request before running {method}");
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
                    SendConfirmationRequest($"This will run {method} on {many} entities in resource " +
                                            $"'{PreviousResultMetadata.Request.Resource.Name}'. ");
                    break;
            }
        }

        private void SendResult(ISerializedResult result, TimeSpan? elapsed = null)
        {
            if (result is SwitchedTerminal) return;
            WebSocket.SendResult(result, WriteStatusBeforeContent, elapsed);
            if (!WriteQueryAfterContent) return;
            switch (result)
            {
                case NoQuery _:
                    WebSocket.SendText("? <empty>");
                    break;
                default:
                    WebSocket.SendText("? " + Query);
                    break;
            }
        }

        private void SendShellInit()
        {
            if (!WriteInfoTexts) return;
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
            if (!WriteInfoTexts) return;
            WebSocket.SendText("### Closing the RESTar WebSocket shell... ###");
            var connection = (WebSocketConnection) WebSocket;
            connection.WebSocket.Disconnect();
        }

        private void SendHelp() => WebSocket.SendText(
            "\n  The RESTar WebSocket shell makes it possible to send\n" +
            "  multiple requests to a RESTar API, over a single TCP\n" +
            "  connection. Using commands, the client can navigate\n" +
            "  around the resources of the API, read, insert, update\n" +
            "  and/or delete entities, or enter terminals. To navigate\n" +
            "  to a resource, simply send a request URI over this WebSocket,\n" +
            "  e.g. '/availableresource//limit=3'. To list the entities,\n" +
            "  send 'GET'. To insert a new entity into a resource, send the\n" +
            "  representation over the WebSocket, for example in JSON. To \n" +
            "  update entities, send 'PATCH <json>',  where <json> is the \n" +
            "  JSON data to update entities from. To delete selected entities,\n" +
            "  send 'DELETE'. For potentially unsafe operations, you will be\n" +
            "  asked to confirm before changes are applied.\n\n" +
            "  Some other simple commands:\n" +
            "  ?           Prints the current location\n" +
            "  REPORT      Counts the entities at the current location\n" +
            "  HELP        Prints this help page\n" +
            "  CLOSE       Closes the WebSocket\n");

        private void SendCredits()
        {
            WebSocket.SendText($"RESTar is designed and developed by Erik von Krusenstierna, © Mopedo AB {DateTime.Now.Year}");
        }
    }
}