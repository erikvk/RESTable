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
    /// <inheritdoc cref="System.IAsyncDisposable" />
    /// <inheritdoc cref="RESTable.Resources.Terminal" />
    /// <summary>
    /// The WebSocket shell, used to navigate and execute commands against RESTable resources
    /// from a connected WebSocket. 
    /// </summary>
    [RESTable(Description = description, GETAvailableToAll = true)]
    public sealed class Shell : Terminal, IAsyncDisposable
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
        private bool _autoOptions;
        private string _protocol;

        private IEntities PreviousEntities { get; set; }

        /// <summary>
        /// Signals that there are changes to the query that have been made pre evaluation
        /// </summary>
        private bool queryChangedPreEval;

        internal static ITerminalResource<Shell> TerminalResource { get; set; }

        private void Reset()
        {
            streamBufferSize = MaxStreamBufferSize;
            Unsafe = false;
            PreviousEntities = null;
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
            Reset();
        }

        public ValueTask DisposeAsync()
        {
            WebSocket.Context.Client.ShellConfig = Providers.Json.Serialize(this);
            Reset();
            return default;
        }

        /// <inheritdoc />
        public override async Task HandleBinaryInput(byte[] input)
        {
            if (!(input?.Length > 0)) return;
            if (Query.Length == 0 || OnConfirm != null)
                await WebSocket.SendResult(new InvalidShellStateForBinaryInput()).ConfigureAwait(false);
            else await SafeOperation(POST, input).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override bool SupportsTextInput { get; }

        /// <inheritdoc />
        public override bool SupportsBinaryInput { get; }

        /// <inheritdoc />
        protected override async Task Open()
        {
            if (WebSocket.Context.Client.ShellConfig is string config)
            {
                Providers.Json.Populate(config, this);
                await SendShellInit().ConfigureAwait(false);
                await SendQuery().ConfigureAwait(false);
            }
            else if (Query != "")
                await Navigate().ConfigureAwait(false);
            else await SendShellInit().ConfigureAwait(false);
        }

        private async Task Navigate(string input = null)
        {
            if (input != null)
                Query = input;
            var (valid, resource) = await ValidateQuery().ConfigureAwait(false);
            if (!valid) return;
            PreviousEntities = null;
            if (AutoOptions) await SendOptions(resource).ConfigureAwait(false);
            else if (AutoGet) await SafeOperation(GET).ConfigureAwait(false);
            else await SendQuery().ConfigureAwait(false);
        }

        private Func<Task> OnConfirm { get; set; }

        /// <inheritdoc />
        public override async Task HandleTextInput(string input)
        {
            if (OnConfirm != null)
            {
                switch (input.FirstOrDefault())
                {
                    case var _ when input.Length > 1:
                    default:
                        await SendConfirmationRequest().ConfigureAwait(false);
                        break;
                    case 'Y':
                    case 'y':
                        await OnConfirm().ConfigureAwait(false);
                        OnConfirm = null;
                        break;
                    case 'N':
                    case 'n':
                        OnConfirm = null;
                        await SendCancel().ConfigureAwait(false);
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
                    await Navigate(input).ConfigureAwait(false);
                    break;
                case '[':
                case '{':
                    await SafeOperation(POST, input.ToBytes()).ConfigureAwait(false);
                    break;
                case var _ when input.Length > MaxInputSize:
                    await SendBadRequest().ConfigureAwait(false);
                    break;
                default:
                    var (command, tail) = input.TSplit(' ');
                    switch (command.ToUpperInvariant())
                    {
                        case "GET":
                            await SafeOperation(GET, tail?.ToBytes()).ConfigureAwait(false);
                            break;
                        case "POST":
                            await SafeOperation(POST, tail?.ToBytes()).ConfigureAwait(false);
                            break;
                        case "PATCH":
                            await UnsafeOperation(PATCH, tail?.ToBytes()).ConfigureAwait(false);
                            break;
                        case "PUT":
                            await SafeOperation(PUT, tail?.ToBytes()).ConfigureAwait(false);
                            break;
                        case "DELETE":
                            await UnsafeOperation(DELETE, tail?.ToBytes()).ConfigureAwait(false);
                            break;
                        case "REPORT":
                            await SafeOperation(REPORT, tail?.ToBytes()).ConfigureAwait(false);
                            break;
                        case "HEAD":
                            await SafeOperation(HEAD, tail?.ToBytes()).ConfigureAwait(false);
                            break;
                        case "STREAM":
                            var result = await WsEvaluate(GET, null).ConfigureAwait(false);
                            var serialized = await result.Serialize().ConfigureAwait(false);
                            if (result is Content)
                                await StreamSerializedResult(serialized, result.TimeElapsed).ConfigureAwait(false);
                            else await SendSerializedResult(serialized).ConfigureAwait(false);
                            break;
                        case "OPTIONS":
                        {
                            var (valid, resource) = await ValidateQuery().ConfigureAwait(false);
                            if (!valid) break;
                            await SendOptions(resource).ConfigureAwait(false);
                            break;
                        }
                        case "SCHEMA":
                        {
                            var (valid, resource) = await ValidateQuery().ConfigureAwait(false);
                            if (!valid) break;
                            var resourceCondition = new Condition<Schema>
                            (
                                key: "resource",
                                op: Operators.EQUALS,
                                value: resource.Name
                            );
                            await using var schemaRequest = WebSocket.Context.CreateRequest<Schema>().WithConditions(resourceCondition);
                            var schemaResult = await schemaRequest.EvaluateToEntities().ConfigureAwait(false);
                            await SerializeAndSendResult(schemaResult);
                            break;
                        }

                        case "HEADERS":
                        case "HEADER":
                            tail = tail?.Trim();
                            if (string.IsNullOrWhiteSpace(tail))
                            {
                                await SendHeaders().ConfigureAwait(false);
                                break;
                            }
                            var (key, value) = tail.TSplit('=', true);
                            if (value == null)
                            {
                                await SendHeaders().ConfigureAwait(false);
                                break;
                            }
                            if (key.IsCustomHeaderName())
                            {
                                if (value == "null")
                                {
                                    WebSocket.Headers.Remove(key);
                                    await SendHeaders().ConfigureAwait(false);
                                    break;
                                }
                                WebSocket.Headers[key] = value;
                                await SendHeaders().ConfigureAwait(false);
                            }
                            else await WebSocket.SendText($"400: Bad request. Cannot read or write reserved header '{key}'.").ConfigureAwait(false);
                            return;

                        case "VAR":
                            if (string.IsNullOrWhiteSpace(tail))
                            {
                                await WebSocket.SendJson(this).ConfigureAwait(false);
                                break;
                            }
                            var (property, valueString) = tail.TSplit('=', true);
                            if (property == null || valueString == null)
                            {
                                await WebSocket.SendText("Invalid property assignment syntax. Should be: VAR <property> = <value>").ConfigureAwait(false);
                                break;
                            }
                            if (valueString.EqualsNoCase("null"))
                                valueString = null;
                            if (!TerminalResource.Members.TryGetValue(property, out var declaredProperty))
                            {
                                await WebSocket.SendText($"Unknown shell property '{property}'. To list properties, type VAR").ConfigureAwait(false);
                                break;
                            }
                            try
                            {
                                declaredProperty.SetValue(this, valueString.ParseConditionValue(declaredProperty));
                                await WebSocket.SendJson(this).ConfigureAwait(false);
                            }
                            catch (Exception e)
                            {
                                await WebSocket.SendException(e).ConfigureAwait(false);
                            }
                            break;
                        case "EXIT":
                        case "QUIT":
                        case "DISCONNECT":
                        case "CLOSE":
                            await Close().ConfigureAwait(false);
                            break;

                        case "GO":
                        case "NAVIGATE":
                        case "?":
                            if (!string.IsNullOrWhiteSpace(tail))
                            {
                                await Navigate(tail).ConfigureAwait(false);
                                break;
                            }
                            await WebSocket.SendText($"{(Query.Any() ? Query : "< empty >")}").ConfigureAwait(false);
                            break;
                        case "FIRST":
                            await Permute(p => p.GetFirstLink(tail.ToNumber() ?? 1)).ConfigureAwait(false);
                            break;
                        case "LAST":
                            await Permute(p => p.GetLastLink(tail.ToNumber() ?? 1)).ConfigureAwait(false);
                            break;
                        case "ALL":
                            await Permute(p => p.GetAllLink()).ConfigureAwait(false);
                            break;
                        case "NEXT":
                            await Permute
                            (
                                asyncPermuter: async p => p.GetNextPageLink
                                (
                                    entityCount: await p.CountAsync().ConfigureAwait(false),
                                    nextPageSize: tail.ToNumber() ?? -1
                                )
                            ).ConfigureAwait(false);
                            break;
                        case "PREV":
                        case "PREVIOUS":
                            await Permute
                            (
                                asyncPermuter: async p => p.GetPreviousPageLink
                                (
                                    entityCount: await p.CountAsync().ConfigureAwait(false),
                                    nextPageSize: tail.ToNumber() ?? -1
                                )
                            ).ConfigureAwait(false);
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

                            await WebSocket.SendText(getHelloWorld()).ConfigureAwait(false);
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

                            await WebSocket.SendText(getGreeting()).ConfigureAwait(false);
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

                            await WebSocket.SendText(getYoureWelcome()).ConfigureAwait(false);
                            break;
                        case "CREDITS":
                            await SendCredits().ConfigureAwait(false);
                            break;
                        case var unknown:
                            await SendUnknownCommand(unknown).ConfigureAwait(false);
                            break;

                        #endregion
                    }
                    break;
            }
        }

        private async Task SendHeaders() => await WebSocket.SendJson(new {WebSocket.Headers}).ConfigureAwait(false);

        private async Task EnsurePreviousEntities()
        {
            if (PreviousEntities == null)
            {
                await WsGetPreliminary().ConfigureAwait(false);
                if (PreviousEntities == null)
                {
                    await SendResult(new ShellNoContent(WebSocket)).ConfigureAwait(false);
                }
            }
        }

        private async Task Permute(Func<IEntities, IUriComponents> permuter)
        {
            await EnsurePreviousEntities().ConfigureAwait(false);
            var link = permuter(PreviousEntities);
            await Navigate(link.ToString()).ConfigureAwait(false);
        }

        private async Task Permute(Func<IEntities, ValueTask<IUriComponents>> asyncPermuter)
        {
            await EnsurePreviousEntities().ConfigureAwait(false);
            var link = await asyncPermuter(PreviousEntities).ConfigureAwait(false);
            await Navigate(link.ToString()).ConfigureAwait(false);
        }

        private async Task<IResult> WsGetPreliminary()
        {
            if (Query.Length == 0) return new ShellNoQuery(WebSocket);
            var local = Query;
            await using var request = WebSocket.Context.CreateRequest(GET, local, null, WebSocket.Headers);
            var result = await request.Evaluate().ConfigureAwait(false);
            switch (result)
            {
                case Results.Error _ when queryChangedPreEval:
                    query = previousQuery;
                    break;
                case IEntities entities:
                    query = local;
                    PreviousEntities = entities;
                    break;
                case Change _:
                    query = local;
                    PreviousEntities = null;
                    break;
                default:
                    query = local;
                    break;
            }
            queryChangedPreEval = false;
            return result;
        }

        private async Task<IResult> WsEvaluate(Method method, byte[] body)
        {
            if (Query.Length == 0) return new ShellNoQuery(WebSocket);
            var local = Query;
            await using var request = WebSocket.Context.CreateRequest(method, local, body, WebSocket.Headers);
            var result = await request.Evaluate().ConfigureAwait(false);
            switch (result)
            {
                case Results.Error _ when queryChangedPreEval:
                    query = previousQuery;
                    break;
                case IEntities entities:
                    query = local;
                    PreviousEntities = entities;
                    break;
                case Change _:
                    query = local;
                    PreviousEntities = null;
                    break;
                default:
                    query = local;
                    break;
            }
            queryChangedPreEval = false;
            return result;
        }

        private async Task<(bool isValid, IResource resource)> ValidateQuery()
        {
            var localQuery = Query;
            if (!WebSocket.Context.UriIsValid(localQuery, out var error, out var resource, out var components))
            {
                query = previousQuery;
                await SendResult(error).ConfigureAwait(false);
                return (false, null);
            }
            if (ReformatQueries)
                localQuery = components.ToString();
            query = localQuery;
            queryChangedPreEval = false;
            return (true, resource);
        }

        private async Task SendQuery() => await WebSocket.SendText("? " + Query).ConfigureAwait(false);

        private async Task SendOptions(IResource resource)
        {
            var availableResource = AvailableResource.Make(resource, WebSocket);
            var options = new OptionsBody(availableResource.Name, availableResource.Kind, availableResource.Methods);
            await WebSocket.SendJson(options, true).ConfigureAwait(false);
            await SendQuery().ConfigureAwait(false);
        }

        private async Task SafeOperation(Method method, byte[] body = null)
        {
            var sw = Stopwatch.StartNew();
            var result = await WsEvaluate(method, body).ConfigureAwait(false);
            await SerializeAndSendResult(result, sw.Elapsed).ConfigureAwait(true);
            sw.Stop();
        }

        private async Task UnsafeOperation(Method method, byte[] body = null)
        {
            async Task runOperation()
            {
                WebSocket.Headers.UnsafeOverride = true;
                await SafeOperation(method, body).ConfigureAwait(false);
            }

            if (PreviousEntities == null)
            {
                var result = await WsEvaluate(GET, null).ConfigureAwait(false);
                if (result is not IEntities)
                {
                    await SendResult(result).ConfigureAwait(false);
                    return;
                }
            }

            switch (await PreviousEntities.CountAsync())
            {
                case 0:
                    await SendBadRequest($". No entities for to run {method} on").ConfigureAwait(false);
                    break;
                case 1:
                    await runOperation().ConfigureAwait(false);
                    break;
                case var multiple:
                    if (Unsafe)
                    {
                        await runOperation().ConfigureAwait(false);
                        break;
                    }
                    OnConfirm = runOperation;
                    await SendConfirmationRequest($"This will run {method} on {multiple} entities in resource " +
                                                  $"'{PreviousEntities.Request.Resource}'. ").ConfigureAwait(false);
                    break;
            }
        }

        private async Task SendResult(IResult result, TimeSpan? elapsed = null)
        {
            if (result is SwitchedTerminal) return;
            await WebSocket.SendResult(result, elapsed, WriteHeaders).ConfigureAwait(false);
            switch (result)
            {
                case var _ when Query == "":
                case ShellNoQuery _:
                    await WebSocket.SendText("? <no query>").ConfigureAwait(false);
                    break;
                default:
                    await WebSocket.SendText("? " + Query).ConfigureAwait(false);
                    break;
            }
        }

        private async Task SerializeAndSendResult(IResult result, TimeSpan? elapsed = null)
        {
            if (result is SwitchedTerminal) return;
            await WebSocket.SendResult(result, elapsed, WriteHeaders).ConfigureAwait(false);
            await using var stream = WebSocket.GetOutputStream(false);
            await result.Serialize(stream);
            switch (result)
            {
                case var _ when Query == "":
                case ShellNoQuery _:
                    await WebSocket.SendText("? <no query>").ConfigureAwait(false);
                    break;
                default:
                    await WebSocket.SendText("? " + Query).ConfigureAwait(false);
                    break;
            }
        }

        private async Task SendSerializedResult(ISerializedResult serializedResult, TimeSpan? elapsed = null)
        {
            if (serializedResult.Result is SwitchedTerminal) return;
            await WebSocket.SendSerializedResult(serializedResult, elapsed, WriteHeaders).ConfigureAwait(false);
            switch (serializedResult.Result)
            {
                case var _ when Query == "":
                case ShellNoQuery _:
                    await WebSocket.SendText("? <no query>").ConfigureAwait(false);
                    break;
                default:
                    await WebSocket.SendText("? " + Query).ConfigureAwait(false);
                    break;
            }
        }

        private async Task StreamSerializedResult(ISerializedResult serializedResult, TimeSpan? elapsed = null)
        {
            if (serializedResult.Result is SwitchedTerminal) return;
            await WebSocket.StreamSerializedResult(serializedResult, StreamBufferSize, elapsed, WriteHeaders).ConfigureAwait(false);
            switch (serializedResult.Result)
            {
                case var _ when Query == "":
                case ShellNoQuery _:
                    await WebSocket.SendText("? <no query>").ConfigureAwait(false);
                    break;
                default:
                    await WebSocket.SendText("? " + Query).ConfigureAwait(false);
                    break;
            }
        }

        private async Task SendShellInit()
        {
            await WebSocket.SendText("### Entering the RESTable WebSocket shell... ###").ConfigureAwait(false);
            await WebSocket.SendText("### Type a command to continue...            ###").ConfigureAwait(false);
        }

        private const string ConfirmationText = "Type 'Y' to continue, 'N' to cancel";
        private const string CancelText = "Operation cancelled";

        private Task SendCancel() => WebSocket.SendText(CancelText);
        private Task SendConfirmationRequest(string initialInfo = null) => WebSocket.SendText(initialInfo + ConfirmationText);
        private Task SendBadRequest(string message = null) => WebSocket.SendText($"400: Bad request{message}");
        private Task SendInvalidCommandArgument(string command, string arg) => WebSocket.SendText($"Invalid argument '{arg}' for command '{command}'");
        private Task SendUnknownCommand(string command) => WebSocket.SendText($"Unknown command '{command}'");
        private Task SendCredits() => WebSocket.SendText($"RESTable is designed and developed by Erik von Krusenstierna, © {DateTime.Now.Year}");

        private async Task Close()
        {
            await WebSocket.SendText("### Closing the RESTable WebSocket shell... ###").ConfigureAwait(false);
            var connection = (WebSocketConnection) WebSocket;
            await connection.WebSocket.DisposeAsync().ConfigureAwait(false);
        }
    }
}