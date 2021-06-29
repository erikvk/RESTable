using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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

        private IEntities? PreviousEntities { get; set; }
        private IJsonProvider JsonProvider { get; }

        /// <summary>
        /// Signals that there are changes to the query that have been made pre evaluation
        /// </summary>
        private bool queryChangedPreEval;

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

        public Shell(IJsonProvider jsonProvider)
        {
            JsonProvider = jsonProvider;
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

        public ValueTask DisposeAsync()
        {
            WebSocket.Context.Client.ShellConfig = JsonProvider.Serialize(this);
            return default;
        }

        /// <inheritdoc />
        public override async Task HandleBinaryInput(Stream input, CancellationToken cancellationToken)
        {
            if (Query.Length == 0 || ConfirmationContinuation is not null)
            {
                if (input is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                // ReSharper disable once MethodHasAsyncOverload
                else input.Dispose();
                await WebSocket.SendResult(new InvalidShellStateForBinaryInput(), cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else await SafeOperation(POST, input, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override bool SupportsTextInput => true;

        /// <inheritdoc />
        protected override bool SupportsBinaryInput => true;


        /// <inheritdoc />
        protected override async Task Open(CancellationToken cancellationToken)
        {
            if (WebSocket.Context.Client.ShellConfig is string config)
            {
                JsonProvider.Populate(config, this);
                await SendShellInit(cancellationToken).ConfigureAwait(false);
                await SendQuery().ConfigureAwait(false);
            }
            else if (Query != "")
                await Navigate().ConfigureAwait(false);
            else await SendShellInit(cancellationToken).ConfigureAwait(false);
        }

        private async Task Navigate(string? input = null, bool sendQuery = true)
        {
            if (input is not null)
                Query = input;
            var (valid, resource) = await ValidateQuery().ConfigureAwait(false);
            if (!valid) return;
            PreviousEntities = null;
            if (AutoOptions) await SendOptions(resource!).ConfigureAwait(false);
            else if (AutoGet) await SafeOperation(GET).ConfigureAwait(false);
            else if (sendQuery) await SendQuery().ConfigureAwait(false);
        }

        private Func<CancellationToken, Task>? ConfirmationContinuation { get; set; }

        /// <inheritdoc />
        public override async Task HandleTextInput(string input, CancellationToken cancellationToken)
        {
            input = input.TrimEnd('\r', '\n');
            if (ConfirmationContinuation is not null)
            {
                switch (input.FirstOrDefault())
                {
                    case var _ when input.Length > 1:
                    default:
                        await SendConfirmRequest(cancellationToken: cancellationToken).ConfigureAwait(false);
                        break;
                    case 'Y':
                    case 'y':
                        await ConfirmationContinuation(cancellationToken).ConfigureAwait(false);
                        ConfirmationContinuation = null;
                        break;
                    case 'N':
                    case 'n':
                        ConfirmationContinuation = null;
                        await SendCancel(cancellationToken).ConfigureAwait(false);
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
                    await SafeOperation(POST, input.ToBytes(), cancellationToken).ConfigureAwait(false);
                    break;
                case var _ when input.Length > MaxInputSize:
                    await SendBadRequest(cancellationToken: cancellationToken).ConfigureAwait(false);
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
                            await SafeOperation(GET, tail?.ToBytes(), cancellationToken).ConfigureAwait(false);
                            break;
                        case "POST":
                            await SafeOperation(POST, tail?.ToBytes(), cancellationToken).ConfigureAwait(false);
                            break;
                        case "PATCH":
                            await UnsafeOperation(PATCH, tail?.ToBytes(), cancellationToken).ConfigureAwait(false);
                            break;
                        case "PUT":
                            await SafeOperation(PUT, tail?.ToBytes(), cancellationToken).ConfigureAwait(false);
                            break;
                        case "DELETE":
                            await UnsafeOperation(DELETE, tail?.ToBytes(), cancellationToken).ConfigureAwait(false);
                            break;
                        case "REPORT":
                            await SafeOperation(REPORT, tail?.ToBytes(), cancellationToken).ConfigureAwait(false);
                            break;
                        case "HEAD":
                            await SafeOperation(HEAD, tail?.ToBytes(), cancellationToken).ConfigureAwait(false);
                            break;
                        case "OBSERVE":
                        {
                            CancellationTokenSource timeoutCancellationTokenSource;
                            if (!string.IsNullOrWhiteSpace(tail) && double.TryParse(tail, out var timeOutSeconds))
                                timeoutCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeOutSeconds));
                            else timeoutCancellationTokenSource = new CancellationTokenSource();
                            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationTokenSource.Token);
                            var _cancellationToken = cancellationTokenSource.Token;

                            await using var result = await GetResult(GET, cancellationToken: _cancellationToken).ConfigureAwait(false);
                            if (result is not IEntities<object> entities)
                            {
                                await SendResult(result, cancellationToken: _cancellationToken).ConfigureAwait(false);
                                break;
                            }
                            await foreach (var entity in entities.WithCancellation(_cancellationToken))
                            {
                                await WebSocket.SendJson(entity, cancellationToken: _cancellationToken).ConfigureAwait(false);
                            }
                            break;
                        }
                        case "STREAM":
                        {
                            await using var result = await GetResult(GET, cancellationToken: cancellationToken).ConfigureAwait(false);
                            var serialized = await result.Serialize(cancellationToken: cancellationToken).ConfigureAwait(false);
                            if (result is Content)
                                await StreamSerializedResult(serialized, result.TimeElapsed, cancellationToken).ConfigureAwait(false);
                            else await SendSerializedResult(serialized, cancellationToken: cancellationToken).ConfigureAwait(false);
                            break;
                        }
                        case "OPTIONS":
                        {
                            var (valid, resource) = await ValidateQuery().ConfigureAwait(false);
                            if (!valid) break;
                            await SendOptions(resource!).ConfigureAwait(false);
                            break;
                        }
                        case "SCHEMA":
                        {
                            var (valid, resource) = await ValidateQuery().ConfigureAwait(false);
                            if (!valid) break;
                            var termFactory = Services.GetRequiredService<TermFactory>();
                            var term = termFactory.MakeConditionTerm(resource!, "resource");
                            var resourceCondition = new Condition<Schema>
                            (
                                term: term,
                                op: Operators.EQUALS,
                                value: resource!.Name
                            );
                            var schemaRequest = WebSocket.Context.CreateRequest<Schema>().WithConditions(resourceCondition);
                            await using var schemaResult = await schemaRequest.GetResultOrThrow<IEntities>(cancellationToken: cancellationToken).ConfigureAwait(false);
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
                                    .SendText($"400: Bad request. Cannot read or write reserved header '{key}'.", cancellationToken)
                                    .ConfigureAwait(false);
                            return;

                        case "VAR":
                            if (string.IsNullOrWhiteSpace(tail))
                            {
                                await WebSocket.SendJson(this, cancellationToken: cancellationToken).ConfigureAwait(false);
                                break;
                            }
                            var (property, valueString) = tail.TupleSplit('=', true);
                            if (valueString is null)
                            {
                                await WebSocket
                                    .SendText("Invalid property assignment syntax. Should be: VAR <property> = <value>", cancellationToken)
                                    .ConfigureAwait(false);
                                break;
                            }
                            if (valueString.EqualsNoCase("null"))
                                valueString = null;
                            if (!TerminalResource.Members.TryGetValue(property, out var declaredProperty))
                            {
                                await WebSocket
                                    .SendText($"Unknown shell property '{property}'. To list properties, type VAR", cancellationToken)
                                    .ConfigureAwait(false);
                                break;
                            }
                            try
                            {
                                await declaredProperty!.SetValue(this, valueString?.ParseConditionValue(declaredProperty)).ConfigureAwait(false);
                                await WebSocket.SendJson(this, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }
                            catch (Exception e)
                            {
                                await WebSocket.SendException(e, cancellationToken).ConfigureAwait(false);
                            }
                            break;
                        case "EXIT":
                        case "QUIT":
                        case "DISCONNECT":
                        case "CLOSE":
                            await Close(cancellationToken).ConfigureAwait(false);
                            break;

                        case "GO":
                        case "NAVIGATE":
                        case "?":
                            if (!string.IsNullOrWhiteSpace(tail))
                            {
                                await Navigate(tail).ConfigureAwait(false);
                                break;
                            }
                            await WebSocket.SendText($"{(Query.Any() ? Query : "< empty >")}", cancellationToken)
                                .ConfigureAwait(false);
                            break;
                        case "FIRST":
                            await Permute(p => p.GetFirstLink(tail.AsNumber() ?? 1)).ConfigureAwait(false);
                            break;
                        case "LAST":
                            await Permute(p => p.GetLastLink(tail.AsNumber() ?? 1)).ConfigureAwait(false);
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
                                    nextPageSize: tail.AsNumber() ?? -1
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
                                    nextPageSize: tail.AsNumber() ?? -1
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

                            await WebSocket.SendText(getHelloWorld(), cancellationToken).ConfigureAwait(false);
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

                            await WebSocket.SendText(getGreeting(), cancellationToken).ConfigureAwait(false);
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

                            await WebSocket.SendText(getYoureWelcome(), cancellationToken).ConfigureAwait(false);
                            break;
                        case "CREDITS":
                            await SendCredits(cancellationToken).ConfigureAwait(false);
                            break;
                        case var unknown:
                            await SendUnknownCommand(unknown, cancellationToken).ConfigureAwait(false);
                            break;

                        #endregion
                    }
                    break;
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
            var link = permuter(PreviousEntities!);
            await Navigate(link.ToString()).ConfigureAwait(false);
        }

        private async Task Permute(Func<IEntities, ValueTask<IUriComponents>> asyncPermuter)
        {
            await EnsurePreviousEntities().ConfigureAwait(false);
            var link = await asyncPermuter(PreviousEntities!).ConfigureAwait(false);
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

        private async Task<IResult> GetResult(Method method, object? body = null, CancellationToken cancellationToken = new())
        {
            if (Query.Length == 0) return new ShellNoQuery(WebSocket);
            var local = Query;
            var request = WebSocket.Context.CreateRequest(method, local, body, WebSocket.Headers);
            var result = await request.GetResult(cancellationToken).ConfigureAwait(false);
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

        private async Task<(bool isValid, IResource? resource)> ValidateQuery()
        {
            var localQuery = Query;
            if (!WebSocket.Context.UriIsValid(localQuery, out var error, out var resource, out var components))
            {
                query = previousQuery;
                await SendResult(error).ConfigureAwait(false);
                return (false, null);
            }
            if (ReformatQueries)
                localQuery = components!.ToString();
            query = localQuery!;
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

        private async Task SafeOperation(Method method, object? body = null, CancellationToken cancellationToken = new())
        {
            var sw = Stopwatch.StartNew();
            await using var result = await GetResult(method, body, cancellationToken).ConfigureAwait(false);
            await SerializeAndSendResult(result, sw.Elapsed).ConfigureAwait(true);
        }

        private async Task UnsafeOperation(Method method, object? body = null, CancellationToken cancellationToken = new())
        {
            async Task runOperation(CancellationToken ct)
            {
                WebSocket.Headers.UnsafeOverride = true;
                await SafeOperation(method, body, ct).ConfigureAwait(false);
            }

            if (PreviousEntities is null)
            {
                await using var result = await GetResult(GET, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (result is not IEntities)
                {
                    await SendResult(result, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return;
                }
            }

            switch (await PreviousEntities!.CountAsync().ConfigureAwait(false))
            {
                case 0:
                {
                    await SendBadRequest($". No entities to run {method} on", cancellationToken).ConfigureAwait(false);
                    break;
                }
                case 1:
                {
                    await runOperation(cancellationToken).ConfigureAwait(false);
                    break;
                }
                case var multiple:
                {
                    if (!Unsafe)
                    {
                        ConfirmationContinuation = runOperation;
                        await SendConfirmRequest
                        (
                            initialInfo: $"This will run {method} on {multiple} entities in resource " +
                                         $"'{PreviousEntities!.Request.Resource}'. ",
                            cancellationToken: cancellationToken
                        ).ConfigureAwait(false);
                        break;
                    }
                    await runOperation(cancellationToken).ConfigureAwait(false);
                    break;
                }
            }
        }

        private async Task SendResult(IResult result, TimeSpan? elapsed = null, CancellationToken cancellationToken = new())
        {
            if (result is SwitchedTerminal) return;
            await WebSocket.SendResult(result, elapsed, WriteHeaders, cancellationToken).ConfigureAwait(false);
            switch (result)
            {
                case var _ when Query == "":
                case ShellNoQuery _:
                {
                    await WebSocket.SendText("? <no query>", cancellationToken).ConfigureAwait(false);
                    break;
                }
                default:
                {
                    await WebSocket.SendText("? " + Query, cancellationToken).ConfigureAwait(false);
                    break;
                }
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

        private async Task SendSerializedResult(ISerializedResult serializedResult, TimeSpan? elapsed = null, CancellationToken cancellationToken = new())
        {
            if (serializedResult.Result is SwitchedTerminal) return;
            await WebSocket.SendSerializedResult(serializedResult, elapsed, WriteHeaders, cancellationToken: cancellationToken).ConfigureAwait(false);
            switch (serializedResult.Result)
            {
                case var _ when Query == "":
                case ShellNoQuery _:
                    await WebSocket.SendText("? <no query>", cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    await WebSocket.SendText("? " + Query, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        private async Task StreamSerializedResult(ISerializedResult serializedResult, TimeSpan? elapsed = null, CancellationToken cancellationToken = new())
        {
            if (serializedResult.Result is SwitchedTerminal) return;
            await WebSocket.StreamSerializedResult(serializedResult, StreamBufferSize, elapsed, WriteHeaders, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            switch (serializedResult.Result)
            {
                case var _ when Query == "":
                case ShellNoQuery _:
                    await WebSocket.SendText("? <no query>", cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    await WebSocket.SendText("? " + Query, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        private async Task SendShellInit(CancellationToken cancellationToken) =>
            await WebSocket.SendText("### Entering the RESTable WebSocket shell... ###", cancellationToken).ConfigureAwait(false);

        private const string ConfirmationText = "Type 'Y' to continue, 'N' to cancel";
        private const string CancelText = "Operation cancelled";

        private Task SendCancel(CancellationToken cancellationToken) => WebSocket.SendText(CancelText, cancellationToken);

        private Task SendConfirmRequest(string? initialInfo = null, CancellationToken cancellationToken = new()) =>
            WebSocket.SendText(initialInfo + ConfirmationText, cancellationToken);

        private Task SendBadRequest(string? message = null, CancellationToken cancellationToken = new()) => WebSocket.SendText($"400: Bad request{message}", cancellationToken);

        private Task SendUnknownCommand(string command, CancellationToken cancellationToken = new()) => WebSocket.SendText($"Unknown command '{command}'", cancellationToken);

        private Task SendCredits(CancellationToken cancellationToken = new()) =>
            WebSocket.SendText($"RESTable is designed and developed by Erik von Krusenstierna, © {DateTime.Now.Year}", cancellationToken);

        private async Task Close(CancellationToken cancellationToken = new())
        {
            await WebSocket.SendText("### Closing the RESTable WebSocket shell... ###", cancellationToken).ConfigureAwait(false);
            var connection = (WebSocketConnection) WebSocket;
            await connection.WebSocket.DisposeAsync().ConfigureAwait(false);
        }
    }
}