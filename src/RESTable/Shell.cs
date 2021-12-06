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
using Error = RESTable.Results.Error;

namespace RESTable;

/// <inheritdoc cref="System.IAsyncDisposable" />
/// <inheritdoc cref="RESTable.Resources.Terminal" />
/// <summary>
///     The WebSocket shell, used to navigate and execute commands against RESTable resources
///     from a connected WebSocket.
/// </summary>
[RESTable(Description = description, GETAvailableToAll = true)]
public sealed class Shell : Terminal, IAsyncDisposable
{
    private const string ConfirmationText = "Type 'Y' to continue, 'N' to cancel";
    private const string CancelText = "Operation cancelled";

    public Shell(IJsonProvider jsonProvider)
    {
        JsonProvider = jsonProvider;
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

    private Func<CancellationToken, Task>? ConfirmationContinuation { get; set; }

    public ValueTask DisposeAsync()
    {
        WebSocket.Context.Client.ShellConfig = JsonProvider.Serialize(this);
        return default;
    }

    /// <inheritdoc />
    public override async Task HandleBinaryInput(Stream input, CancellationToken cancellationToken)
    {
        if (Query.Length == 0 || ConfirmationContinuation is not null)
            await WebSocket.SendResult(new InvalidShellStateForBinaryInput(), cancellationToken: cancellationToken).ConfigureAwait(false);
        else await SafeOperation(POST, input, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task Open(CancellationToken cancellationToken)
    {
        if (WebSocket.Context.Client.ShellConfig is string config)
        {
            await JsonProvider.PopulateAsync(this, config).ConfigureAwait(false);
            await SendShellInit(cancellationToken).ConfigureAwait(false);
            await SendQuery(cancellationToken).ConfigureAwait(false);
        }
        else if (Query != "")
        {
            await Navigate(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            Query = $"/{TerminalResource.Name}";
            await SendShellInit(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task Navigate(string? input = null, bool sendQuery = true, CancellationToken cancellationToken = new())
    {
        if (input is not null)
            Query = input;
        var (valid, resource) = await ValidateQuery().ConfigureAwait(false);
        if (!valid) return;
        PreviousEntities = null;
        if (AutoOptions) await SendOptions(resource!, cancellationToken).ConfigureAwait(false);
        else if (AutoGet) await SafeOperation(GET, cancellationToken: cancellationToken).ConfigureAwait(false);
        else if (sendQuery) await SendQuery(cancellationToken).ConfigureAwait(false);
    }

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

        if (string.IsNullOrWhiteSpace(input))
            input = "GET";

        switch (input.FirstOrDefault())
        {
            case '\0':
            case '\n': break;
            case '-':
            case '/':
                await Navigate(input, cancellationToken: cancellationToken).ConfigureAwait(false);
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
                        await Navigate(path, false, cancellationToken).ConfigureAwait(false);
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
                        var acceptProvider = WebSocket.GetOutputContentTypeProvider();

                        await using var result = await GetResult(GET, cancellationToken: _cancellationToken).ConfigureAwait(false);
                        await WebSocket.SendResult(result, null, WriteHeaders, cancellationToken).ConfigureAwait(false);
                        if (result is not IEntities<object> entities)
                            break;
                        await foreach (var entity in entities.WithCancellation(_cancellationToken))
                        {
                            var entityData = acceptProvider.SerializeToBytes(entity, entities.EntityType);
                            await WebSocket.Send(entityData, true, _cancellationToken).ConfigureAwait(false);
                        }
                        break;
                    }
                    case "OPTIONS":
                    {
                        var (valid, resource) = await ValidateQuery().ConfigureAwait(false);
                        if (!valid) break;
                        await SendOptions(resource!, cancellationToken).ConfigureAwait(false);
                        break;
                    }
                    case "SCHEMA":
                    {
                        var (valid, resource) = await ValidateQuery().ConfigureAwait(false);
                        if (!valid) break;
                        var schemaRequest = WebSocket.Context
                            .CreateRequest<Schema>()
                            .WithAddedCondition("$resource", Operators.EQUALS, resource!.Name);
                        await using var schemaResult = await schemaRequest.GetResultOrThrow<IEntities>(cancellationToken).ConfigureAwait(false);
                        await SerializeAndSendResult(schemaResult, cancellationToken: cancellationToken).ConfigureAwait(false);
                        break;
                    }
                    case "HEADERS":
                    case "HEADER":
                        tail = tail?.Trim();
                        if (string.IsNullOrWhiteSpace(tail))
                        {
                            await SendHeaders(cancellationToken).ConfigureAwait(false);
                            break;
                        }
                        var (key, value) = tail.TupleSplit('=', true);
                        if (value is null)
                        {
                            await SendHeaders(cancellationToken).ConfigureAwait(false);
                            break;
                        }
                        if (key.IsCustomHeaderName())
                        {
                            if (value == "null")
                            {
                                WebSocket.Headers.Remove(key);
                                await SendHeaders(cancellationToken).ConfigureAwait(false);
                                break;
                            }
                            WebSocket.Headers[key] = value;
                            await SendHeaders(cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            await WebSocket
                                .SendText($"400: Bad request. Cannot read or write reserved header '{key}'.", cancellationToken)
                                .ConfigureAwait(false);
                        }
                        return;

                    case "VAR":
                        if (string.IsNullOrWhiteSpace(tail))
                        {
                            await SendJson(this, cancellationToken).ConfigureAwait(false);
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
                            await declaredProperty.SetValue(this, valueString?.ParseConditionValue(declaredProperty)).ConfigureAwait(false);
                            await SendJson(this, cancellationToken).ConfigureAwait(false);
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
                            await Navigate(tail, cancellationToken: cancellationToken).ConfigureAwait(false);
                            break;
                        }
                        await WebSocket.SendText($"{(Query.Any() ? Query : "< empty >")}", cancellationToken)
                            .ConfigureAwait(false);
                        break;
                    case "FIRST":
                        await GetLinkAndNavigate(p => p.GetFirstLink(tail.AsNumber() ?? 1), cancellationToken).ConfigureAwait(false);
                        break;
                    case "LAST":
                        await GetLinkAndNavigate(p => p.GetLastLink(tail.AsNumber() ?? 1), cancellationToken).ConfigureAwait(false);
                        break;
                    case "ALL":
                        await GetLinkAndNavigate(p => p.GetAllLink(), cancellationToken).ConfigureAwait(false);
                        break;
                    case "NEXT":
                        await GetLinkAndNavigate(async p => p.GetNextPageLink
                        (
                            await p.CountAsync().ConfigureAwait(false),
                            tail.AsNumber() ?? -1
                        ), cancellationToken).ConfigureAwait(false);
                        break;
                    case "PREV":
                    case "PREVIOUS":
                        await GetLinkAndNavigate(async p => p.GetPreviousPageLink
                        (
                            await p.CountAsync().ConfigureAwait(false),
                            tail.AsNumber() ?? -1
                        ), cancellationToken).ConfigureAwait(false);
                        break;

                    #region Nonsense

                    case "HELLO," when tail.EqualsNoCase("world!"):

                        string getHelloWorld()
                        {
                            return new Random().Next(0, 7) switch
                            {
                                0 => "The world says: 'hi!'",
                                1 => "The world says: 'what's up?'",
                                2 => "The world says: 'greetings!'",
                                3 => "The world is currently busy",
                                4 => "The world cannot answer right now",
                                5 => "The world is currently out on lunch",
                                _ => "The world says: 'why do people keep saying that?'"
                            };
                        }

                        await WebSocket.SendText(getHelloWorld(), cancellationToken).ConfigureAwait(false);
                        break;

                    case "HI":
                    case "HELLO":

                        string getGreeting()
                        {
                            return new Random().Next(0, 10) switch
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
                        }

                        await WebSocket.SendText(getGreeting(), cancellationToken).ConfigureAwait(false);
                        break;
                    case "NICE":
                    case "THANKS":
                    case "THANK":

                        string getYoureWelcome()
                        {
                            return new Random().Next(0, 7) switch
                            {
                                0 => "😎",
                                1 => "👍",
                                2 => "🙌",
                                3 => "🎉",
                                4 => "🤘",
                                5 => "You're welcome!",
                                _ => "✌️"
                            };
                        }

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

    private async ValueTask<bool> EnsurePreviousEntities()
    {
        if (PreviousEntities is null)
        {
            await WsGetPreliminary().ConfigureAwait(false);
            if (PreviousEntities is null) return false;
        }
        return true;
    }

    private async Task GetLinkAndNavigate(Func<IEntities, ValueTask<IUriComponents>> linkSelector, CancellationToken cancellationToken)
    {
        var hasContent = await EnsurePreviousEntities().ConfigureAwait(false);
        if (!hasContent)
            return;
        var link = await linkSelector(PreviousEntities!).ConfigureAwait(false);
        await Navigate(link.ToString(), cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task<IResult> WsGetPreliminary()
    {
        var local = Query;
        var request = WebSocket.Context.CreateRequest(GET, local, null, WebSocket.Headers);
        await using var result = await request.GetResult().ConfigureAwait(false);
        switch (result)
        {
            case Error _ when queryChangedPreEval:
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
        var local = Query;
        var request = WebSocket.Context.CreateRequest(method, local, WebSocket.Headers, body);
        var result = await request.GetResult(cancellationToken).ConfigureAwait(false);
        switch (result)
        {
            case Error _ when queryChangedPreEval:
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
            await SendResult(error!).ConfigureAwait(false);
            return (false, null);
        }
        if (ReformatQueries)
            localQuery = components!.ToString();
        query = localQuery ?? throw new NullReferenceException(nameof(localQuery));
        queryChangedPreEval = false;
        return (true, resource);
    }

    private Task SendQuery(CancellationToken cancellationToken)
    {
        return WebSocket.SendText("? " + Query, cancellationToken);
    }

    private async Task SendOptions(IResource resource, CancellationToken cancellationToken)
    {
        var availableResource = AvailableResource.Make(resource, WebSocket);
        var options = new OptionsBody(availableResource.Name, availableResource.Kind, availableResource.Methods);
        await SendJson(options, cancellationToken).ConfigureAwait(false);
        await SendQuery(cancellationToken).ConfigureAwait(false);
    }

    private async Task SafeOperation(Method method, object? body = null, CancellationToken cancellationToken = new())
    {
        var sw = Stopwatch.StartNew();
        await using var result = await GetResult(method, body, cancellationToken).ConfigureAwait(false);
        await SerializeAndSendResult(result, sw.Elapsed, cancellationToken).ConfigureAwait(true);
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
                        $"This will run {method} on {multiple} entities in resource " +
                        $"'{PreviousEntities!.Request.Resource}'. ",
                        cancellationToken
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
            {
                await WebSocket.SendText("? <no query>", cancellationToken).ConfigureAwait(false);
                break;
            }
            default:
            {
                await SendQuery(cancellationToken).ConfigureAwait(false);
                break;
            }
        }
    }

    private async Task SerializeAndSendResult(IResult result, TimeSpan? elapsed = null, CancellationToken cancellationToken = new())
    {
        if (result is SwitchedTerminal) return;
        await WebSocket.SendResult(result, elapsed, WriteHeaders, cancellationToken).ConfigureAwait(false);

        var message = await WebSocket.GetMessageStream(false, cancellationToken).ConfigureAwait(false);
#if NETSTANDARD2_0
        using (message)
#else
        await using (message.ConfigureAwait(false))
#endif
        {
            await using var serialized = await result.Serialize(message, cancellationToken).ConfigureAwait(false);
        }
        await SendQuery(cancellationToken).ConfigureAwait(false);
    }

    private Task SendHeaders(CancellationToken cancellationToken)
    {
        return SendJson(new {WebSocket.Headers}, cancellationToken);
    }

    private Task SendJson<T>(T obj, CancellationToken cancellationToken)
    {
        var shellData = JsonProvider.SerializeToUtf8Bytes(obj, true, true);
        return WebSocket.Send(shellData, true, cancellationToken);
    }

    private Task SendShellInit(CancellationToken cancellationToken)
    {
        return WebSocket.SendText("### Entering the RESTable WebSocket shell... ###", cancellationToken);
    }

    private Task SendCancel(CancellationToken cancellationToken)
    {
        return WebSocket.SendText(CancelText, cancellationToken);
    }

    private Task SendConfirmRequest(string? initialInfo = null, CancellationToken cancellationToken = new())
    {
        return WebSocket.SendText(initialInfo + ConfirmationText, cancellationToken);
    }

    private Task SendBadRequest(string? message = null, CancellationToken cancellationToken = new())
    {
        return WebSocket.SendText($"400: Bad request{message}", cancellationToken);
    }

    private Task SendUnknownCommand(string command, CancellationToken cancellationToken = new())
    {
        return WebSocket.SendText($"Unknown command '{command}'", cancellationToken);
    }

    private Task SendCredits(CancellationToken cancellationToken = new())
    {
        return WebSocket.SendText($"RESTable is designed and developed by Erik von Krusenstierna, © {DateTime.Now.Year}", cancellationToken);
    }

    private async Task Close(CancellationToken cancellationToken = new())
    {
        await WebSocket.SendText("### Closing the RESTable WebSocket shell... ###", cancellationToken).ConfigureAwait(false);
        var connection = (WebSocketConnection) WebSocket;
        await connection.WebSocket.DisposeAsync().ConfigureAwait(false);
    }

    #region Private

    private const string description =
        "The RESTable WebSocket shell lets the client navigate around the resources of the " +
        "RESTable API, perform CRUD operations and enter terminal resources.";

    private const int MaxInputSize = 16_000_000;

    private string query;
    private string previousQuery;
    private bool _autoGet;
    private bool _autoOptions;
    private string _protocol;

    private IEntities? PreviousEntities { get; set; }
    private IJsonProvider JsonProvider { get; }

    /// <summary>
    ///     Signals that there are changes to the query that have been made pre evaluation
    /// </summary>
    private bool queryChangedPreEval;

    #endregion

    #region Terminal properties

    /// <summary>
    ///     The query to perform in the shell
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
    ///     Should unsafe commands be allowed?
    /// </summary>
    public bool Unsafe { get; set; }

    /// <summary>
    ///     Should the shell write result headers?
    /// </summary>
    public bool WriteHeaders { get; set; }

    /// <summary>
    ///     Should the shell write options after a succesful navigation?
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
    ///     Should the shell automatically run a GET operation after a successful navigation?
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
    ///     Should queries be reformatted after input?
    /// </summary>
    public bool ReformatQueries { get; set; }

    /// <summary>
    ///     The protocol to use in queries
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
}