using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Results;
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

        private string query;
        private string previousQuery;
        private const long MaxStreamBufferSize = 16_000_000;
        private long streamBufferSize;
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
        /// Should unsafe commands be allowed?
        /// </summary>
        public bool Unsafe { get; set; }

        /// <summary>
        /// Should the shell write result headers?
        /// </summary>
        public bool WriteHeaders { get; set; }

        private bool _autoOptions;

        /// <summary>
        /// Should the shell write options after a succesful navigation?
        /// </summary>
        public bool AutoOptions
        {
            get => _autoOptions;
            set
            {
                if (value) _autoGet = false;
                _autoOptions = value;
            }
        }

        private bool _autoGet;

        /// <summary>
        /// Should the shell automatically run a GET operation after a successful navigation?
        /// </summary>
        public bool AutoGet
        {
            get => _autoGet;
            set
            {
                if (value) _autoOptions = false;
                _autoGet = value;
            }
        }

        /// <summary>
        /// The size of stream messages in bytes
        /// </summary>
        public long StreamBufferSize
        {
            get => streamBufferSize;
            set
            {
                if (value < 512 || MaxStreamBufferSize < value)
                    streamBufferSize = MaxStreamBufferSize;
                else streamBufferSize = value;
                if (CurrentStreamManifest != null)
                    SetupStreamManifest();
            }
        }

        /// <inheritdoc />
        public Shell()
        {
            SupportsTextInput = true;
            SupportsBinaryInput = true;
            Reset();
        }

        private void Reset()
        {
            streamBufferSize = 16_000_000;
            Unsafe = false;
            OnConfirm = null;
            PreviousResultMetadata = null;
            GetNextPageLink = null;
            query = "";
            previousQuery = "";
            CurrentStreamManifest?.Dispose();
            WriteHeaders = false;
            AutoOptions = false;
            AutoGet = false;
        }

        /// <inheritdoc />
        public void Dispose() => Reset();

        /// <inheritdoc />
        public IWebSocket WebSocket { private get; set; }

        /// <inheritdoc />
        public void HandleBinaryInput(byte[] input)
        {
            if (!(input?.Length > 0)) return;
            if (Query.Length == 0 || OnConfirm != null)
                WebSocket.SendResult(new InvalidShellStateForBinaryInput());
            else SafeOperation(POST, input);
        }

        /// <inheritdoc />
        public bool SupportsTextInput { get; }

        /// <inheritdoc />
        public bool SupportsBinaryInput { get; }

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
                    case "OPTIONS":
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
                        WebSocket.SendText($"499: Client closed request. Streamed {CurrentStreamManifest.CurrentMessageIndex} " +
                                           $"of {CurrentStreamManifest.NrOfMessages} messages.");
                        CurrentStreamManifest.Dispose();
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
                    if (!QueryIsValid(out var resource)) break;
                    if (AutoOptions) SendOptions(resource);
                    else if (AutoGet) SafeOperation(GET);
                    else SendQuery();
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
                        case "OPTIONS":
                            if (!QueryIsValid(out var _resource)) break;
                            SendOptions(_resource);
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
                        case "SET":
                            if (string.IsNullOrWhiteSpace(tail))
                            {
                                WebSocket.SendJson(this);
                                break;
                            }
                            var parts = Regex.Split(tail, " to ", RegexOptions.IgnoreCase);
                            if (parts.Length == 1)
                            {
                                WebSocket.SendText("Invalid property assignment syntax. Should be: SET <property> TO <value>");
                                break;
                            }
                            var (property, valueString) = (parts[0].Trim(), parts[1]);
                            if (valueString.EqualsNoCase("null"))
                                valueString = null;
                            if (!TerminalResource.Members.TryGetValue(property, out var declaredProperty))
                            {
                                WebSocket.SendText($"Unknown shell property '{property}'. To list properties, type SET");
                                break;
                            }
                            try
                            {
                                declaredProperty.SetValue(this, valueString.ParseConditionValue(declaredProperty));
                                WebSocket.SendJson(this);
                            }
                            catch (Exception e)
                            {
                                WebSocket.SendException(e);
                            }
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
                        case "?" when !string.IsNullOrWhiteSpace(tail):
                            if (tail.Length == 1)
                                tail = "/restar.availableresource";
                            if (tail.StartsWith("//"))
                                tail = $"/restar.availableresource/{tail.Substring(2)}";
                            Query = tail;
                            if (!QueryIsValid(out var __resource)) break;
                            if (AutoOptions) SendOptions(__resource);
                            else SendQuery();
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
                                SendResult(new ShellNoContent(WebSocket, stopwatch.Elapsed));
                            else
                            {
                                Query = link;
                                SafeOperation(GET);
                            }
                            break;

                        #region Nonsense

                        case "HELLO" when tail.EqualsNoCase("world"):

                            string getHelloWorld()
                            {
                                switch (new Random().Next(0, 7))
                                {
                                    case 0: return "The world says: 'hi!'";
                                    case 1: return "The world says: 'what's up?'";
                                    case 2: return "The world says: 'greetings!'";
                                    case 3: return "The world is currently busy";
                                    case 4: return "The world cannot answer right now";
                                    case 5: return "The world is currently out on lunch";
                                    default: return "The world says: 'why do people keep saying that?'";
                                }
                            }

                            WebSocket.SendText(getHelloWorld());
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

                        #endregion
                    }
                    break;
            }
        }

        private byte[] buffer;

        private async void StreamMessage(int nr)
        {
            try
            {
                var startIndex = CurrentStreamManifest.CurrentMessageIndex;
                var endIndex = startIndex + nr;
                if (endIndex > CurrentStreamManifest.NrOfMessages - 1)
                    endIndex = CurrentStreamManifest.NrOfMessages - 1;
                buffer = buffer ?? new byte[StreamBufferSize];
                for (var i = startIndex; i <= endIndex; i += 1)
                {
                    CurrentStreamManifest.CurrentMessageIndex = i;
                    var message = CurrentStreamManifest.Messages[i];
                    await CurrentStreamManifest.Content.Body.ReadAsync(buffer, 0, buffer.Length);
                    WebSocket.SendBinary(buffer, 0, (int) message.Length);
                    message.Sent = true;
                    CurrentStreamManifest.MessagesStreamed += 1;
                    CurrentStreamManifest.MessagesRemaining -= 1;
                    CurrentStreamManifest.BytesStreamed += message.Length;
                    CurrentStreamManifest.BytesRemaining -= message.Length;
                }
                if (endIndex == CurrentStreamManifest.NrOfMessages - 1)
                {
                    WebSocket.SendText($"200: OK. {CurrentStreamManifest.NrOfMessages} messages sucessfully streamed.");
                    CurrentStreamManifest.Dispose();
                    CurrentStreamManifest = null;
                    buffer = null;
                }
            }
            catch (Exception e)
            {
                WebSocket.SendException(e);
                WebSocket.SendText($"500: Error during streaming. Streamed {CurrentStreamManifest?.CurrentMessageIndex ?? 0} " +
                                   $"of {CurrentStreamManifest?.NrOfMessages ?? 1} messages.");
                CurrentStreamManifest?.Dispose();
                CurrentStreamManifest = null;
                buffer = null;
            }
        }

        private void SendHeaders() => WebSocket.SendJson(new {WebSocket.Headers});

        private ISerializedResult WsEvaluate(Method method, byte[] body)
        {
            if (Query.Length == 0) return new ShellNoQuery(WebSocket);
            var local = Query;
            var result = WebSocket.Context.CreateRequest(method, local, body, WebSocket.Headers).Result.Serialize();
            switch (result)
            {
                case Error _ when queryChangedPreEval:
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

        private bool QueryIsValid(out IResource resource)
        {
            var localQuery = Query;
            if (!WebSocket.Context.UriIsValid(localQuery, out var error, out resource))
            {
                query = previousQuery;
                SendResult(error);
                return false;
            }
            query = localQuery;
            queryChangedPreEval = false;
            return true;
        }

        private void SendQuery() => WebSocket.SendText("? " + Query);

        private void SendOptions(IResource resource)
        {
            var availableResource = AvailableResource.Make(resource, WebSocket);
            WebSocket.SendJson(new
            {
                Resource = availableResource.Name,
                ResourceKind = availableResource.Kind,
                availableResource.Methods
            }, true);
            SendQuery();
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
            buffer = null;
        }

        private void SafeOperation(Method method, byte[] body = null)
        {
            var sw = Stopwatch.StartNew();
            switch (WsEvaluate(method, body))
            {
                case Content tooLarge when tooLarge.Body is FileStream || tooLarge.Body.Length > 16_000_000:
                    OnConfirm = () =>
                    {
                        CurrentStreamManifest = new StreamManifest(tooLarge);
                        SetupStreamManifest();
                    };
                    SendConfirmationRequest("426: The response message is too large. Do you wish to stream the response? ");
                    break;
                case OK ok:
                    SendResult(ok, sw.Elapsed);
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
            WebSocket.SendResult(result, elapsed, WriteHeaders);
            switch (result)
            {
                case var _ when Query == "":
                case ShellNoQuery _:
                    WebSocket.SendText("? <no query>");
                    break;
                default:
                    WebSocket.SendText("? " + Query);
                    break;
            }
        }

        private void SendShellInit()
        {
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
            WebSocket.SendText("### Closing the RESTar WebSocket shell... ###");
            var connection = (WebSocketConnection) WebSocket;
            connection.WebSocket.Disconnect();
        }

        private void SendHelp() => WebSocket.SendText("Shell documentation is available at https://github.com/Mopedo/Home/" +
                                                      "blob/master/RESTar/Built-in%20resources/RESTar/Shell.md");

        private void SendCredits()
        {
            WebSocket.SendText($"RESTar is designed and developed by Erik von Krusenstierna, © Mopedo AB {DateTime.Now.Year}");
        }
    }
}