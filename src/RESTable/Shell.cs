using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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

        internal static ITerminalResource<Shell> ShellTerminalResource { get; set; }

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
            ConfirmSource = new TaskCompletionSource<byte>();
            ConfirmSource.SetResult(default);
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
                var provider = Services
                    .GetRequiredService<ProtocolProviderManager>()
                    .ResolveCachedProtocolProvider(value)
                    .ProtocolProvider;
                _protocol = provider.ProtocolIdentifier;
            }
        }

        #endregion

        public Shell()
        {
            Reset();
        }

        public ValueTask DisposeAsync()
        {
            WebSocket.Context.Client.ShellConfig = JsonProvider.Serialize(this);
            Reset();
            return default;
        }

        /// <inheritdoc />
        public override async Task HandleBinaryInput(Stream input)
        {
            if (Query.Length == 0 || AwaitingConfirmation)
                await WebSocket.SendResult(new InvalidShellStateForBinaryInput()).ConfigureAwait(false);
            else await SafeOperation(POST, input).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override bool SupportsTextInput => true;

        /// <inheritdoc />
        protected override bool SupportsBinaryInput => true;

        private IJsonProvider JsonProvider { get; set; }

        /// <inheritdoc />
        protected override async Task Open()
        {
            JsonProvider = Services.GetRequiredService<IJsonProvider>();
            if (WebSocket.Context.Client.ShellConfig is string config)
            {
                JsonProvider.Populate(config, this);
                await SendShellInit().ConfigureAwait(false);
                await SendQuery().ConfigureAwait(false);
            }
            else if (Query != "")
                await Navigate().ConfigureAwait(false);
            else await SendShellInit().ConfigureAwait(false);
        }

        private async Task Navigate(string input = null, bool sendQuery = true)
        {
            if (input is not null)
                Query = input;
            var (valid, resource) = await ValidateQuery().ConfigureAwait(false);
            if (!valid) return;
            PreviousEntities = null;
            if (AutoOptions) await SendOptions(resource).ConfigureAwait(false);
            else if (AutoGet) await SafeOperation(GET).ConfigureAwait(false);
            else if (sendQuery) await SendQuery().ConfigureAwait(false);
        }

        private TaskCompletionSource<byte> ConfirmSource { get; set; }
        private bool AwaitingConfirmation => !ConfirmSource.Task.IsCompleted;

        private async Task Confirm(string message)
        {
            ConfirmSource = new TaskCompletionSource<byte>();
            await SendConfirmRequest(message).ConfigureAwait(false);
            await ConfirmSource.Task.ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async Task HandleTextInput(string input)
        {
            try
            {
                if (AwaitingConfirmation)
                {
                    switch (input.FirstOrDefault())
                    {
                        case var _ when input.Length > 1:
                        default:
                            await SendConfirmRequest().ConfigureAwait(false);
                            break;
                        case 'Y':
                        case 'y':
                            ConfirmSource.SetResult(default);
                            break;
                        case 'N':
                        case 'n':
                            ConfirmSource.SetCanceled();
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
                        var (command, tail) = input.TupleSplit(' ');
                        if (tail is not null)
                        {
                            var (path, tail2) = tail.TupleSplit(' ');
                            if (path.StartsWith("/"))
                            {
                                await Navigate(path).ConfigureAwait(false);
                                tail = tail2;
                            }
                        }
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
                            {
                                await using var result = await GetResult(GET, null).ConfigureAwait(false);
                                var serialized = await result.Serialize().ConfigureAwait(false);
                                if (result is Content)
                                    await StreamSerializedResult(serialized, result.TimeElapsed).ConfigureAwait(false);
                                else await SendSerializedResult(serialized).ConfigureAwait(false);
                                break;
                            }
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
                                var termFactory = Services.GetRequiredService<TermFactory>();
                                var term = termFactory.MakeConditionTerm(resource, "resource");
                                var resourceCondition = new Condition<Schema>
                                (
                                    term: term,
                                    op: Operators.EQUALS,
                                    value: resource.Name
                                );
                                var schemaRequest = WebSocket.Context.CreateRequest<Schema>().WithConditions(resourceCondition);
                                await using var schemaResult = await schemaRequest.GetResultEntities().ConfigureAwait(false);
                                await SerializeAndSendResult(schemaResult).ConfigureAwait(false);
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
                                var (key, value) = tail.TupleSplit('=', true);
                                if (value is null)
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
                                else
                                    await WebSocket
                                        .SendText($"400: Bad request. Cannot read or write reserved header '{key}'.")
                                        .ConfigureAwait(false);
                                return;

                            case "VAR":
                                if (string.IsNullOrWhiteSpace(tail))
                                {
                                    await WebSocket.SendJson(this).ConfigureAwait(false);
                                    break;
                                }
                                var (property, valueString) = tail.TupleSplit('=', true);
                                if (property is null || valueString is null)
                                {
                                    await WebSocket
                                        .SendText(
                                            "Invalid property assignment syntax. Should be: VAR <property> = <value>")
                                        .ConfigureAwait(false);
                                    break;
                                }
                                if (valueString.EqualsNoCase("null"))
                                    valueString = null;
                                if (!ShellTerminalResource.Members.TryGetValue(property, out var declaredProperty))
                                {
                                    await WebSocket
                                        .SendText($"Unknown shell property '{property}'. To list properties, type VAR")
                                        .ConfigureAwait(false);
                                    break;
                                }
                                try
                                {
                                    await declaredProperty!.SetValue(this, valueString.ParseConditionValue(declaredProperty)).ConfigureAwait(false);
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
                                await WebSocket.SendText($"{(Query.Any() ? Query : "< empty >")}")
                                    .ConfigureAwait(false);
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

                            case "HELLO" when tail.EqualsNoCase(", world!"):

                                string getHelloWorld() => new Random().Next(0, 7) switch
                                {
                                    0 => "The world says: 'hi!'",
                                    1 => "The world says: 'what's up?'",
                                    2 => "The world says: 'greetings!'",
                                    3 => "The world is currently busy",
                                    4 => "The world cannot answer right now",
                                    5 => "The world is currently out on lunch",
                                    _ => "The world says: 'why do people keep saying that?'"
                                };

                                await WebSocket.SendText(getHelloWorld()).ConfigureAwait(false);
                                break;

                            case "HI":
                            case "HELLO":

                                string getGreeting() => new Random().Next(0, 10) switch
                                {
                                    0 => "Well, hello there :D",
                                    1 => "Greetings, friend",
                                    2 => "Hello, dear client",
                                    3 => "Hello to you",
                                    4 => "Hi!",
                                    5 => "Nice to see you!",
                                    6 => "What's up?",
                                    7 => "✌️",
                                    8 => "'sup",
                                    _ => "Oh no, it's you again..."
                                };

                                await WebSocket.SendText(getGreeting()).ConfigureAwait(false);
                                break;
                            case "NICE":
                            case "THANKS":
                            case "THANK":

                                string getYoureWelcome() => new Random().Next(0, 7) switch
                                {
                                    0 => "😎",
                                    1 => "👍",
                                    2 => "🙌",
                                    3 => "🎉",
                                    4 => "🤘",
                                    5 => "You're welcome!",
                                    _ => "✌️"
                                };

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
            catch (TaskCanceledException)
            {
                await SendCancel().ConfigureAwait(false);
            }
        }

        private async Task SendHeaders() => await WebSocket.SendJson(new {WebSocket.Headers}).ConfigureAwait(false);

        private async Task EnsurePreviousEntities()
        {
            if (PreviousEntities is null)
            {
                await WsGetPreliminary().ConfigureAwait(false);
                if (PreviousEntities is null)
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
            var request = WebSocket.Context.CreateRequest(GET, local, null, WebSocket.Headers);
            await using var result = await request.GetResult().ConfigureAwait(false);
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

        private async Task<IResult> GetResult(Method method, object body)
        {
            if (Query.Length == 0) return new ShellNoQuery(WebSocket);
            var local = Query;
            var request = WebSocket.Context.CreateRequest(method, local, body, WebSocket.Headers);
            var result = await request.GetResult().ConfigureAwait(false);
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
        
        private async Task SafeOperation(Method method, object body = null)
        {
            var sw = Stopwatch.StartNew();
            await using var result = await GetResult(method, body).ConfigureAwait(false);
            await SerializeAndSendResult(result, sw.Elapsed).ConfigureAwait(true);
        }

        private async Task UnsafeOperation(Method method, byte[] body = null)
        {
            async Task runOperation()
            {
                WebSocket.Headers.UnsafeOverride = true;
                await SafeOperation(method, body).ConfigureAwait(false);
            }

            if (PreviousEntities is null)
            {
                await using var result = await GetResult(GET, null).ConfigureAwait(false);
                if (result is not IEntities)
                {
                    await SendResult(result).ConfigureAwait(false);
                    return;
                }
            }

            switch (await PreviousEntities.CountAsync().ConfigureAwait(false))
            {
                case 0:
                    await SendBadRequest($". No entities to run {method} on").ConfigureAwait(false);
                    break;
                case 1:
                    await runOperation().ConfigureAwait(false);
                    break;
                case var multiple:
                    if (!Unsafe)
                    {
                        await Confirm($"This will run {method} on {multiple} entities in resource " +
                                      $"'{PreviousEntities.Request.Resource}'. ").ConfigureAwait(false);
                    }
                    await runOperation().ConfigureAwait(false);
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
            var message = await WebSocket.GetMessageStream(false).ConfigureAwait(false);
#if NETSTANDARD2_0
            using (message)
#else
            await using (message.ConfigureAwait(false))
#endif
            {
                await using var serialized = await result.Serialize(message).ConfigureAwait(false);
            }
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
            await WebSocket.StreamSerializedResult(serializedResult, StreamBufferSize, elapsed, WriteHeaders)
                .ConfigureAwait(false);
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

        private Task SendConfirmRequest(string initialInfo = null) =>
            WebSocket.SendText(initialInfo + ConfirmationText);

        private Task SendBadRequest(string message = null) => WebSocket.SendText($"400: Bad request{message}");

        private Task SendUnknownCommand(string command) => WebSocket.SendText($"Unknown command '{command}'");

        private Task SendCredits() =>
            WebSocket.SendText($"RESTable is designed and developed by Erik von Krusenstierna, © {DateTime.Now.Year}");

        private async Task Close()
        {
            await WebSocket.SendText("### Closing the RESTable WebSocket shell... ###").ConfigureAwait(false);
            var connection = (WebSocketConnection) WebSocket;
            await connection.WebSocket.DisposeAsync().ConfigureAwait(false);
        }
    }
}