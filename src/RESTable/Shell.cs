using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using RESTable.Admin;
using RESTable.ContentTypeProviders;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Results;
using RESTable.WebSockets;
using static RESTable.ErrorCodes;
using static RESTable.Method;

namespace RESTable
{
    /// <inheritdoc />
    /// <summary>
    /// The WebSocket shell, used to navigate and execute commands against RESTable resources
    /// from a connected WebSocket. 
    /// </summary>
    [RESTable(Description = description, GETAvailableToAll = true)]
    public sealed class Shell : ITerminal
    {
        #region Private

        private const string description =
            "The RESTable WebSocket shell lets the client navigate around the resources of the " +
            "RESTable API, perform CRUD operations and enter terminal resources.";

        private const int MaxStreamBufferSize = 16_000_000;
        private const int MinStreamBufferSize = 512;
        private const int ResultStreamThreshold = 16_000_000;
        private const int MaxInputSize = 16_000_000;

        private string query;
        private string previousQuery;
        private bool _autoGet;
        private int streamBufferSize;
        private IEntities _previousEntities;
        private bool _autoOptions;
        private string _protocol;

        private IEntities GetPreviousEntities() => _previousEntities;

        private async Task SetPreviousEntities(IEntities value)
        {
            if (_previousEntities != null && value?.Equals(_previousEntities) != true)
                await _previousEntities.DisposeAsync();
            _previousEntities = value;
        }

        /// <summary>
        /// Signals that there are changes to the query that have been made pre evaluation
        /// </summary>
        private bool queryChangedPreEval;

        internal static ITerminalResource<Shell> TerminalResource { get; set; }

        private async Task Reset()
        {
            streamBufferSize = MaxStreamBufferSize;
            Unsafe = false;
            await SetPreviousEntities(null);
            query = "";
            previousQuery = "";
            WriteHeaders = false;
            AutoOptions = false;
            AutoGet = false;
            ReformatQueries = true;
            _protocol = "";
        }

        #endregion

        #region Terminal properties

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
                if (_protocol != "" && value.FirstOrDefault() != '-')
                    query = $"-{_protocol}{value}";
                else query = value;
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
        public int StreamBufferSize
        {
            get => streamBufferSize;
            set
            {
                if (value < MinStreamBufferSize)
                    streamBufferSize = MinStreamBufferSize;
                else if (MaxStreamBufferSize < value)
                    streamBufferSize = MaxStreamBufferSize;
                else streamBufferSize = value;
            }
        }

        /// <summary>
        /// Should queries be reformatted after input?
        /// </summary>
        public bool ReformatQueries { get; set; }

        /// <summary>
        /// The protocol to use in queries
        /// </summary>
        public string Protocol
        {
            get => _protocol == "" ? "restable" : _protocol;
            set
            {
                var p = ProtocolController.ResolveProtocolProvider(value).ProtocolProvider;
                _protocol = p.ProtocolIdentifier;
            }
        }

        #endregion

        public Shell()
        {
            SupportsTextInput = true;
            SupportsBinaryInput = true;
            Reset().Wait();
        }

        public async ValueTask DisposeAsync()
        {
            WebSocket.Context.Client.ShellConfig = Providers.Json.Serialize(this);
            await Reset();
        }

        /// <inheritdoc />
        public IWebSocket WebSocket { private get; set; }

        /// <inheritdoc />
        public async Task HandleBinaryInput(byte[] input)
        {
            if (!(input?.Length > 0)) return;
            if (Query.Length == 0 || OnConfirm != null)
                await WebSocket.SendResult(new InvalidShellStateForBinaryInput());
            else await SafeOperation(POST, input);
        }

        /// <inheritdoc />
        public bool SupportsTextInput { get; }

        /// <inheritdoc />
        public bool SupportsBinaryInput { get; }

        /// <inheritdoc />
        public async Task Open()
        {
            if (WebSocket.Context.Client.ShellConfig is string config)
            {
                Providers.Json.Populate(config, this);
                await SendShellInit();
                await SendQuery();
            }
            else if (Query != "")
                await Navigate();
            else await SendShellInit();
        }

        private async Task Navigate(string input = null)
        {
            if (input != null)
                Query = input;
            var (valid, resource) = await ValidateQuery();
            if (!valid) return;
            await SetPreviousEntities(null);
            if (AutoOptions) await SendOptions(resource);
            else if (AutoGet) await SafeOperation(GET);
            else await SendQuery();
        }

        private Func<Task> OnConfirm { get; set; }

        /// <inheritdoc />
        public async Task HandleTextInput(string input)
        {
            if (OnConfirm != null)
            {
                switch (input.FirstOrDefault())
                {
                    case var _ when input.Length > 1:
                    default:
                        await SendConfirmationRequest();
                        break;
                    case 'Y':
                    case 'y':
                        await OnConfirm();
                        OnConfirm = null;
                        break;
                    case 'N':
                    case 'n':
                        OnConfirm = null;
                        await SendCancel();
                        break;
                }
                return;
            }

            if (input == " ")
                input = "GET";

            switch (input.FirstOrDefault())
            {
                case '\0':
                case '\n': break;
                case '-':
                case '/':
                    await Navigate(input);
                    break;
                case '[':
                case '{':
                    await SafeOperation(POST, input.ToBytes());
                    break;
                case var _ when input.Length > MaxInputSize:
                    await SendBadRequest();
                    break;
                default:
                    var (command, tail) = input.TSplit(' ');
                    switch (command.ToUpperInvariant())
                    {
                        case "GET":
                            await SafeOperation(GET, tail?.ToBytes());
                            break;
                        case "POST":
                            await SafeOperation(POST, tail?.ToBytes());
                            break;
                        case "PATCH":
                            await UnsafeOperation(PATCH, tail?.ToBytes());
                            break;
                        case "PUT":
                            await SafeOperation(PUT, tail?.ToBytes());
                            break;
                        case "DELETE":
                            await UnsafeOperation(DELETE, tail?.ToBytes());
                            break;
                        case "REPORT":
                            await SafeOperation(REPORT, tail?.ToBytes());
                            break;
                        case "HEAD":
                            await SafeOperation(HEAD, tail?.ToBytes());
                            break;
                        case "STREAM":
                            var result = await WsEvaluate(GET, null);
                            if (result is Content)
                                await StreamResult(result, result.TimeElapsed);
                            else await SendResult(result);
                            break;
                        case "OPTIONS":
                        {
                            var (valid, resource) = await ValidateQuery();
                            if (!valid) break;
                            await SendOptions(resource);
                            break;
                        }
                        case "SCHEMA":
                        {
                            var (valid, resource) = await ValidateQuery();
                            if (!valid) break;
                            var resourceCondition = new Condition<Schema>
                            (
                                key: "resource",
                                op: Operators.EQUALS,
                                value: resource.Name
                            );
                            var schema = await WebSocket.Context
                                .CreateRequest<Schema>()
                                .WithConditions(resourceCondition)
                                .EvaluateToEntities();
                            await SendResult(schema);
                            break;
                        }

                        case "HEADERS":
                        case "HEADER":
                            tail = tail?.Trim();
                            if (string.IsNullOrWhiteSpace(tail))
                            {
                                await SendHeaders();
                                break;
                            }
                            var (key, value) = tail.TSplit('=', true);
                            if (value == null)
                            {
                                await SendHeaders();
                                break;
                            }
                            if (key.IsCustomHeaderName())
                            {
                                if (value == "null")
                                {
                                    WebSocket.Headers.Remove(key);
                                    await SendHeaders();
                                    break;
                                }
                                WebSocket.Headers[key] = value;
                                await SendHeaders();
                            }
                            else await WebSocket.SendText($"400: Bad request. Cannot read or write reserved header '{key}'.");
                            return;

                        case "VAR":
                            if (string.IsNullOrWhiteSpace(tail))
                            {
                                await WebSocket.SendJson(this);
                                break;
                            }
                            var (property, valueString) = tail.TSplit('=', true);
                            if (property == null || valueString == null)
                            {
                                await WebSocket.SendText("Invalid property assignment syntax. Should be: VAR <property> = <value>");
                                break;
                            }
                            if (valueString.EqualsNoCase("null"))
                                valueString = null;
                            if (!TerminalResource.Members.TryGetValue(property, out var declaredProperty))
                            {
                                await WebSocket.SendText($"Unknown shell property '{property}'. To list properties, type VAR");
                                break;
                            }
                            try
                            {
                                declaredProperty.SetValue(this, valueString.ParseConditionValue(declaredProperty));
                                await WebSocket.SendJson(this);
                            }
                            catch (Exception e)
                            {
                                await WebSocket.SendException(e);
                            }
                            break;
                        case "EXIT":
                        case "QUIT":
                        case "DISCONNECT":
                        case "CLOSE":
                            await Close();
                            break;

                        case "GO":
                        case "NAVIGATE":
                        case "?":
                            if (!string.IsNullOrWhiteSpace(tail))
                            {
                                await Navigate(tail);
                                break;
                            }
                            await WebSocket.SendText($"{(Query.Any() ? Query : "< empty >")}");
                            break;
                        case "FIRST":
                            await Permute(p => p.GetFirstLink(tail.ToNumber() ?? 1));
                            break;
                        case "LAST":
                            await Permute(p => p.GetLastLink(tail.ToNumber() ?? 1));
                            break;
                        case "ALL":
                            await Permute(p => p.GetAllLink());
                            break;
                        case "NEXT":
                            await Permute(p => p.GetNextPageLink(tail.ToNumber() ?? -1));
                            break;
                        case "PREV":
                        case "PREVIOUS":
                            await Permute(p => p.GetPreviousPageLink(tail.ToNumber() ?? -1));
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

                            await WebSocket.SendText(getHelloWorld());
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

                            await WebSocket.SendText(getGreeting());
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

                            await WebSocket.SendText(getYoureWelcome());
                            break;
                        case "CREDITS":
                            await SendCredits();
                            break;
                        case var unknown:
                            await SendUnknownCommand(unknown);
                            break;

                        #endregion
                    }
                    break;
            }
        }

        private async Task SendHeaders() => await WebSocket.SendJson(new {WebSocket.Headers});

        private async Task Permute(Func<IEntities, IUriComponents> permuter)
        {
            var stopwatch = Stopwatch.StartNew();
            if (GetPreviousEntities() == null)
            {
                var preliminary = await WsGetPreliminary();
                await preliminary.DisposeAsync();
                if (GetPreviousEntities() == null)
                {
                    await SendResult(new ShellNoContent(WebSocket, stopwatch.Elapsed));
                    return;
                }
            }
            var link = permuter(GetPreviousEntities());
            await Navigate(link.ToString());
        }

        private async Task<IResult> WsGetPreliminary()
        {
            if (Query.Length == 0) return new ShellNoQuery(WebSocket);
            var local = Query;
            await using var request = WebSocket.Context.CreateRequest(local, GET, null, WebSocket.Headers);
            var result = await request.Evaluate();
            switch (result)
            {
                case Results.Error _ when queryChangedPreEval:
                    query = previousQuery;
                    break;
                case IEntities entities:
                    query = local;
                    await SetPreviousEntities(entities);
                    break;
                case Change _:
                    query = local;
                    await SetPreviousEntities(null);
                    break;
                default:
                    query = local;
                    break;
            }
            queryChangedPreEval = false;
            return result;
        }

        private async Task<ISerializedResult> WsEvaluate(Method method, byte[] body)
        {
            if (Query.Length == 0) return new ShellNoQuery(WebSocket);
            var local = Query;
            await using var request = WebSocket.Context.CreateRequest(local, method, body, WebSocket.Headers);
            var result = await request.Evaluate();
            var serialized = result.Serialize().Serialize();
            switch (serialized)
            {
                case Results.Error _ when queryChangedPreEval:
                    query = previousQuery;
                    break;
                case IEntities entities:
                    query = local;
                    await SetPreviousEntities(entities);
                    break;
                case Change _:
                    query = local;
                    await SetPreviousEntities(null);
                    break;
                default:
                    query = local;
                    break;
            }
            queryChangedPreEval = false;
            return serialized;
        }

        private async Task<(bool isValid, IResource resource)> ValidateQuery()
        {
            var localQuery = Query;
            if (!WebSocket.Context.UriIsValid(localQuery, out var error, out var resource, out var components))
            {
                query = previousQuery;
                await SendResult(error);
                return (false, null);
            }
            if (ReformatQueries)
                localQuery = components.ToString();
            query = localQuery;
            queryChangedPreEval = false;
            return (true, resource);
        }

        private async Task SendQuery() => await WebSocket.SendText("? " + Query);

        private async Task SendOptions(IResource resource)
        {
            var availableResource = AvailableResource.Make(resource, WebSocket);
            var options = new OptionsBody(availableResource.Name, availableResource.Kind, availableResource.Methods);
            await WebSocket.SendJson(options, true);
            await SendQuery();
        }

        private async Task SafeOperation(Method method, byte[] body = null)
        {
            var sw = Stopwatch.StartNew();
            switch (await WsEvaluate(method, body))
            {
                case Content {Body: Body} content:

                    if (!content.Body.CanRead)
                    {
                        await SendResult(new UnreadableContentStream(content));
                        break;
                    }
                    await content.Body.MakeSeekable();
                    if (content.Body.Length > ResultStreamThreshold)
                    {
                        OnConfirm = () => StreamResult(content, sw.Elapsed);
                        await SendConfirmationRequest("426: The result body is too large to be sent in a single WebSocket message. " +
                                                      "Do you want to stream the result instead? ");
                        break;
                    }
                    await SendResult(content, sw.Elapsed);
                    break;
                case OK ok:
                    await SendResult(ok, sw.Elapsed);
                    break;
                case var other:
                    await SendResult(other);
                    break;
            }
            sw.Stop();
        }

        private async Task UnsafeOperation(Method method, byte[] body = null)
        {
            async Task runOperation()
            {
                WebSocket.Headers.UnsafeOverride = true;
                await SafeOperation(method, body);
            }

            if (GetPreviousEntities() == null)
            {
                var result = await WsEvaluate(GET, null);
                if (result is IEntities entities)
                    await SetPreviousEntities(entities);
                else
                {
                    await SendResult(result);
                    return;
                }
            }

            switch (GetPreviousEntities().EntityCount)
            {
                case 0:
                    await SendBadRequest($". No entities for to run {method} on");
                    break;
                case 1:
                    await runOperation();
                    break;
                case var multiple:
                    if (Unsafe)
                    {
                        await runOperation();
                        break;
                    }
                    OnConfirm = runOperation;
                    await SendConfirmationRequest($"This will run {method} on {multiple} entities in resource " +
                                                  $"'{GetPreviousEntities().Request.Resource}'. ");
                    break;
            }
        }

        private async Task SendResult(ISerializedResult result, TimeSpan? elapsed = null)
        {
            if (result is SwitchedTerminal) return;
            await WebSocket.SendResult(result, elapsed, WriteHeaders);
            switch (result)
            {
                case var _ when Query == "":
                case ShellNoQuery _:
                    await WebSocket.SendText("? <no query>");
                    break;
                default:
                    await WebSocket.SendText("? " + Query);
                    break;
            }
        }

        private async Task StreamResult(ISerializedResult result, TimeSpan? elapsed = null)
        {
            if (result is SwitchedTerminal) return;
            await WebSocket.StreamResult(result, StreamBufferSize, elapsed, WriteHeaders);
            switch (result)
            {
                case var _ when Query == "":
                case ShellNoQuery _:
                    await WebSocket.SendText("? <no query>");
                    break;
                default:
                    await WebSocket.SendText("? " + Query);
                    break;
            }
        }

        private async Task SendShellInit()
        {
            await WebSocket.SendText("### Entering the RESTable WebSocket shell... ###");
            await WebSocket.SendText("### Type a command to continue...            ###");
        }

        private const string ConfirmationText = "Type 'Y' to continue, 'N' to cancel";
        private const string CancelText = "Operation cancelled";

        private async Task SendCancel() => await WebSocket.SendText(CancelText);
        private async Task SendConfirmationRequest(string initialInfo = null) => await WebSocket.SendText(initialInfo + ConfirmationText);
        private async Task SendBadRequest(string message = null) => await WebSocket.SendText($"400: Bad request{message}");
        private async Task SendInvalidCommandArgument(string command, string arg) => await WebSocket.SendText($"Invalid argument '{arg}' for command '{command}'");
        private async Task SendUnknownCommand(string command) => await WebSocket.SendText($"Unknown command '{command}'");
        private async Task SendCredits() => await WebSocket.SendText($"RESTable is designed and developed by Erik von Krusenstierna, © Mopedo AB {DateTime.Now.Year}");

        private async Task Close()
        {
            await WebSocket.SendText("### Closing the RESTable WebSocket shell... ###");
            var connection = (WebSocketConnection) WebSocket;
            await connection.WebSocket.DisposeAsync();
        }
    }

    internal static class ShellExtensions
    {
        internal static int? ToNumber(this string tail)
        {
            if (tail == null || !int.TryParse(tail, out var nr))
                return null;
            return nr;
        }
    }
}