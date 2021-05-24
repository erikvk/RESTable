using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Auth;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Meta.Internal;
using RESTable.Resources.Operations;
using RESTable.Results;
using static RESTable.ErrorCodes;
using static RESTable.Method;
using Error = RESTable.Results.Error;

namespace RESTable.Requests
{
    internal class Request<T> : IRequest, IRequest<T>, IEntityRequest<T>, ITraceable where T : class
    {
        public RequestParameters Parameters { get; }
        public IResource<T> Resource { get; }
        public ITarget<T> Target { get; }
        ITarget IRequest.Target => Target;
        private Exception Error { get; set; }
        private bool IsEvaluating { get; set; }
        private Headers _responseHeaders;
        public Headers ResponseHeaders => _responseHeaders ??= new Headers();

        private List<Condition<T>> _conditions;

        public List<Condition<T>> Conditions
        {
            get => _conditions ??= new List<Condition<T>>();
            set => _conditions = value;
        }

        private MetaConditions _metaConditions;

        public MetaConditions MetaConditions
        {
            get => _metaConditions ??= new MetaConditions();
            set => _metaConditions = value;
        }

        #region Forwarding properties

        public Cookies Cookies => Context.Client.Cookies;
        public bool HasConditions => !(_conditions?.Count > 0);
        public bool IsValid => Error is null;
        public string ProtocolIdentifier => Parameters.ProtocolIdentifier;
        public TimeSpan TimeElapsed => Stopwatch.Elapsed;
        private Stopwatch Stopwatch { get; }
        IEntityResource<T> IEntityRequest<T>.EntityResource => Resource as IEntityResource<T>;

        public Func<IAsyncEnumerable<T>> GetSelector() => Selector;
        public Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>> GetUpdater() => Updater;

        public Func<IAsyncEnumerable<T>> Selector { private get; set; }
        public Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>> Updater { private get; set; }

        public Func<IAsyncEnumerable<T>> EntitiesProducer { get; set; }

        public IEnumerable<T> GetInputEntities() => GetInputEntitiesAsync().ToEnumerable();

        public async IAsyncEnumerable<T> GetInputEntitiesAsync()
        {
            if (EntitiesProducer is null) yield break;
            await foreach (var item in EntitiesProducer().ConfigureAwait(false))
                yield return item;
        }

        IResource IRequest.Resource => Resource;
        public Headers Headers => Parameters.Headers;
        public RESTableContext Context => Parameters.Context;
        public bool IsWebSocketUpgrade => Parameters.IsWebSocketUpgrade;
        public IMacro Macro => Parameters.UriComponents.Macro;
        private ILogable LogItem => Parameters;
        private IHeaderHolder HeaderHolder => Parameters;
        MessageType ILogable.MessageType => LogItem.MessageType;
        ValueTask<string> ILogable.GetLogMessage() => LogItem.GetLogMessage();
        ValueTask<string> ILogable.GetLogContent() => LogItem.GetLogContent();
        public DateTime LogTime { get; } = DateTime.Now;
        bool IHeaderHolder.ExcludeHeaders => HeaderHolder.ExcludeHeaders;
        private RESTableConfiguration Configuration { get; }

        public Body Body
        {
            get => Parameters.Body;
            set
            {
                if (IsEvaluating)
                    throw new InvalidOperationException("Cannot set the request body whilst the request is evaluating");
                Parameters.Body = value;
            }
        }

        public CachedProtocolProvider CachedProtocolProvider
        {
            get => Parameters.CachedProtocolProvider;
            private set => Parameters.CachedProtocolProvider = value;
        }


        public Method Method
        {
            get => Parameters.Method;
            set
            {
                if (IsEvaluating) return;
                Parameters.Method = value;
            }
        }


        public IUriComponents UriComponents => new UriComponents
        (
            resourceSpecifier: Resource.Name,
            viewName: Target is IView ? Target.Name : null,
            conditions: Conditions,
            metaConditions: MetaConditions.GetEnumerable(),
            protocolProvider: CachedProtocolProvider.ProtocolProvider,
            macro: Parameters.UriComponents.Macro
        );

        string IHeaderHolder.HeadersStringCache
        {
            get => HeaderHolder.HeadersStringCache;
            set => HeaderHolder.HeadersStringCache = value;
        }

        #endregion

        public TData GetClientData<TData>(string key)
        {
            if (Context.Client.ResourceClientDataMappings.TryGetValue(Resource, out var data) && data.TryGetValue(key, out var value))
                return (TData) value;
            return default;
        }

        public void SetClientData<TData>(string key, TData value)
        {
            if (!Context.Client.ResourceClientDataMappings.TryGetValue(Resource, out var data))
                data = Context.Client.ResourceClientDataMappings[Resource] = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            data[key] = value;
        }

        public async Task<IEntities<T>> GetResultEntities()
        {
            var result = await GetResult().ConfigureAwait(false);
            if (result is Error e) throw e;
            return (IEntities<T>) result;
        }

        public async Task<IResult> GetResult(CancellationToken cancellationToken = new())
        {
            cancellationToken.ThrowIfCancellationRequested();
            Stopwatch.Restart();
            var sourceDelegate = GetSourceDelegate();
            var destinationDelegate = GetDestinationDelegate();
            var result = GetQuickErrorResult();

            if (result is null)
            {
                Body = await sourceDelegate(Body).ConfigureAwait(false);
                await Body.Initialize(cancellationToken).ConfigureAwait(false);
                result = await Execute(cancellationToken).ConfigureAwait(false);
            }

            if (IsWebSocketUpgrade && result is not WebSocketUpgradeSuccessful)
            {
                var webSocket = Context.WebSocket;
                await using (webSocket.ConfigureAwait(false))
                {
                    if (result is Forbidden forbidden)
                        return new WebSocketUpgradeFailed(forbidden);
                    await Context.WebSocket.Open(this, false).ConfigureAwait(false);
                    await webSocket.SendResult(result).ConfigureAwait(false);
                    var message = await webSocket.GetMessageStream(false).ConfigureAwait(false);
#if NETSTANDARD2_1
                    await using (message.ConfigureAwait(false))
#else
                    using (message)
#endif
                    {
                        await result.Serialize(message, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                    return new WebSocketTransferSuccess(this);
                }
            }

            result = await destinationDelegate(result).ConfigureAwait(false);

            if (result is InfiniteLoop loop && !Context.IsBottomOfStack)
                throw loop;

            if (Headers.Metadata == "full" && result.Metadata is string metadata)
                result.Headers.Metadata = metadata;
            result.Headers.Version = Configuration.Version;
            result.Headers.Elapsed = result.TimeElapsed;
            return result;
        }

        private async Task<IResult> Execute(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                Context.IncreaseDepth();
                IsEvaluating = true;

                switch (Resource)
                {
                    case ITerminalResource<T> terminalResource:
                    {
                        if (!Context.HasWebSocket)
                            throw new UpgradeRequired(terminalResource.Name);
                        if (IsWebSocketUpgrade)
                        {
                            var terminalResourceInternal = (TerminalResource<T>) terminalResource;
                            var terminal = terminalResourceInternal.MakeTerminal(Context, Conditions);
                            await Context.WebSocket.Open(this).ConfigureAwait(false);
                            await Context.WebSocket.ConnectTo(terminal).ConfigureAwait(false);
                            await terminal.OpenTerminal().ConfigureAwait(false);
                            return new WebSocketUpgradeSuccessful(this, Context.WebSocket);
                        }
                        return await SwitchTerminal(terminalResource).ConfigureAwait(false);
                    }

                    case IBinaryResource<T> binaryResource:
                    {
                        var binaryResult = binaryResource.SelectBinary(this);
                        if (!this.Accepts(binaryResult.ContentType, out var acceptHeader))
                            throw new NotAcceptable(acceptHeader, binaryResult.ContentType.ToString());
                        var binaryContent = new Binary(this, binaryResult);
                        return binaryContent;
                    }

                    case IEntityResource<T> entityResource:
                    {
                        if (entityResource.RequiresAuthentication)
                        {
                            var authenticator = this.GetRequiredService<ResourceAuthenticator>();
                            await authenticator.ResourceAuthenticate(this, entityResource).ConfigureAwait(false);
                        }
                        if (MetaConditions.SafePost is not null)
                        {
                            if (!entityResource.CanSelect) throw new SafePostNotSupported("(no selector implemented)");
                            if (!entityResource.CanUpdate) throw new SafePostNotSupported("(no updater implemented)");
                        }
                        var evaluator = EntityOperations<T>.GetMethodEvaluator(Method);
                        var result = await evaluator(this).ConfigureAwait(false);
                        foreach (var (key, value) in ResponseHeaders)
                            result.Headers[key.StartsWith("X-") ? key : "X-" + key] = value;
                        if (this.GetRequiredService<IAllowedCorsOriginsFilter>() is AllCorsOriginsAllowed)
                            result.Headers.AccessControlAllowOrigin = "*";
                        else if (Headers.Origin is string origin)
                            result.Headers.AccessControlAllowOrigin = origin;
                        return result;
                    }

                    case var other: throw new UnknownResource(other.Name);
                }
            }
            catch (Exception exception)
            {
                var result = exception.AsResultOf(this);
                foreach (var (key, value) in ResponseHeaders)
                    result.Headers[key.StartsWith("X-") ? key : "X-" + key] = value;
                return result;
            }
            finally
            {
                Context.DecreaseDepth();
                IsEvaluating = false;
            }
        }

        private IResult GetQuickErrorResult()
        {
            if (IsEvaluating)
                throw new InfiniteLoop();
            if (!IsValid)
                return Error.AsResultOf(this);
            if (!Context.MethodIsAllowed(Method, Resource, out var error))
                return error.AsResultOf(this);
            if (IsWebSocketUpgrade)
            {
                try
                {
                    if (!CachedProtocolProvider.ProtocolProvider.IsCompliant(this, out var reason))
                        return new NotCompliantWithProtocol(CachedProtocolProvider.ProtocolProvider, reason).AsResultOf(this);
                }
                catch (NotImplementedException) { }
            }
            return null;
        }

        private async Task<IResult> SwitchTerminal(ITerminalResource<T> resource)
        {
            var _resource = (TerminalResource<T>) resource;
            var newTerminal = _resource.MakeTerminal(Context, Conditions);
            await Context.WebSocket.ConnectTo(newTerminal).ConfigureAwait(false);
            await newTerminal.OpenTerminal().ConfigureAwait(false);
            return new SwitchedTerminal(this);
        }

        internal Request(IResource<T> resource, RequestParameters parameters)
        {
            Parameters = parameters;
            Resource = resource;
            Target = resource;
            Body = parameters.Body;
            Stopwatch = new Stopwatch();
            var termFactory = this.GetRequiredService<TermFactory>();
            Configuration = this.GetRequiredService<RESTableConfiguration>();

            try
            {
                if (resource.IsInternal && Context.Client.Origin != OriginType.Internal)
                    throw new ResourceIsInternal(resource);
                if (Resource is IEntityResource<T> entityResource)
                {
                    MetaConditions = MetaConditions.Parse(parameters.UriComponents.MetaConditions, entityResource, termFactory);
                    if (parameters.UriComponents.ViewName is not null)
                    {
                        if (!entityResource.ViewDictionary.TryGetValue(parameters.UriComponents.ViewName, out var view))
                            throw new UnknownView(parameters.UriComponents.ViewName, entityResource);
                        Target = view;
                    }
                }
                if (parameters.UriComponents.Conditions.Count > 0)
                {
                    var cache = this.GetRequiredService<ConditionCache<T>>();
                    Conditions = Condition<T>.Parse(parameters.UriComponents.Conditions, Target, termFactory, cache);
                }
                if (parameters.Headers.UnsafeOverride)
                {
                    MetaConditions.Unsafe = true;
                    parameters.Headers.UnsafeOverride = false;
                }
            }
            catch (Exception e)
            {
                Error = e;
            }
        }

        private Request
        (
            IResource<T> resource,
            ITarget<T> target,
            CachedProtocolProvider cachedProtocolProvider,
            RequestParameters parameters,
            Body body,
            MetaConditions metaConditions,
            List<Condition<T>> conditions,
            Exception error
        )
        {
            Parameters = parameters;
            Resource = resource;
            Target = target;
            Body = body;
            MetaConditions = metaConditions;
            Conditions = conditions;
            Error = error;
            CachedProtocolProvider = cachedProtocolProvider;
            Stopwatch = new Stopwatch();
        }

        #region Source and destination

        private Func<IResult, Task<IResult>> SendToDestinationDelegate(string destinationHeader)
        {
            if (!HeaderRequestParameters.TryParse(this, nameof(Headers.Destination), destinationHeader, out var parameters, out var parseError))
            {
                Error = parseError;
                return Task.FromResult;
            }

            if (parameters.IsInternal)
            {
                return async result =>
                {
                    var serializedResult = await result.Serialize().ConfigureAwait(false);
                    var internalRequest = serializedResult.Context.CreateRequest
                    (
                        method: parameters.Method,
                        uri: parameters.Uri,
                        body: serializedResult.Body,
                        headers: parameters.Headers
                    );
                    return await internalRequest.GetResult().ConfigureAwait(false);
                };
            }
            return async result =>
            {
                var externalRequest = new HttpRequest(result, parameters, async stream =>
                {
                    await result.Serialize(stream).ConfigureAwait(false);
                });
                var response = await externalRequest.GetResponseAsync().ConfigureAwait(false)
                               ?? throw new InvalidExternalDestination(externalRequest, "No response");
                if (response.StatusCode >= HttpStatusCode.BadRequest)
                    throw new InvalidExternalDestination(externalRequest,
                        $"Received {response.StatusCode.ToCode()} - {response.StatusDescription}. {response.Headers.Info}");
                if (result.Headers.AccessControlAllowOrigin is string h)
                    response.Headers.AccessControlAllowOrigin = h;
                return new ExternalDestinationResult(result.Request, response);
            };
        }

        private Func<Body, Task<Body>> GetBodyFromSourceDelegate(string sourceHeader)
        {
            if (!HeaderRequestParameters.TryParse(this, nameof(Headers.Source), sourceHeader, out var parameters, out var parseError))
            {
                Error = parseError;
                return Task.FromResult;
            }
            if (parameters.Method != GET)
            {
                Error = new InvalidSyntax(InvalidSource, "Only GET is allowed in Source headers");
                return Task.FromResult;
            }

            return async body =>
            {
                if (body is not null)
                    await body.DisposeAsync().ConfigureAwait(false);
                if (parameters.IsInternal)
                {
                    var internalRequest = Context.CreateRequest
                    (
                        method: parameters.Method,
                        uri: parameters.Uri,
                        body: null,
                        headers: parameters.Headers
                    );
                    var result = await internalRequest.GetResult().ConfigureAwait(false);
                    if (result is not IEntities)
                        throw new InvalidExternalSource(parameters.Uri, await result.GetLogMessage().ConfigureAwait(false));
                    var serialized = await result.Serialize().ConfigureAwait(false);
                    if (serialized.Result is Error error) throw error;
                    if (serialized.EntityCount == 0) throw new InvalidExternalSource(parameters.Uri, "Response was empty");
                    return serialized.Body;
                }
                parameters.Headers.Accept ??= ContentType.JSON;
                var request = new HttpRequest(this, parameters, null);
                var response = await request.GetResponseAsync().ConfigureAwait(false);
                if (response is null)
                    throw new InvalidExternalSource(parameters.Uri, "No response");
                if (response.StatusCode >= HttpStatusCode.BadRequest) throw new InvalidExternalSource(parameters.Uri, response.LogMessage);

                if (response.Body.CanSeek && response.Body.Length == 0)
                    throw new InvalidExternalSource(parameters.Uri, "Response was empty");
                return new Body(this, response.Body);
            };
        }

        private Func<Body, Task<Body>> GetSourceDelegate() => Headers.Source switch
        {
            string sourceHeader => GetBodyFromSourceDelegate(sourceHeader),
            _ => Task.FromResult
        };

        private Func<IResult, Task<IResult>> GetDestinationDelegate() => Headers.Destination switch
        {
            string destinationHeader => SendToDestinationDelegate(destinationHeader),
            _ => Task.FromResult
        };

        #endregion

        public async Task<IRequest> GetCopy(string newProtocol = null)
        {
            var protocolController = this.GetRequiredService<ProtocolProviderManager>();
            return new Request<T>
            (
                resource: Resource,
                target: Target,
                cachedProtocolProvider: newProtocol is not null
                    ? protocolController.ResolveCachedProtocolProvider(newProtocol)
                    : CachedProtocolProvider,
                parameters: Parameters,
                body: await Body.GetCopy().ConfigureAwait(false),
                metaConditions: MetaConditions.GetCopy(),
                conditions: Conditions.ToList(),
                error: Error
            );
        }

        public object GetService(Type serviceType) => Context.Services.GetService(serviceType);

        public void Dispose() => Body.Dispose();

        public async ValueTask DisposeAsync() => await Body.DisposeAsync().ConfigureAwait(false);
    }
}