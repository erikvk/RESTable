using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Auth;
using RESTable.ContentTypeProviders;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Meta.Internal;
using RESTable.Resources.Operations;
using RESTable.Results;
using RESTable.WebSockets;

namespace RESTable.Requests
{
    internal class Request<T> : IRequest, IRequest<T>, IEntityRequest<T>, ITraceable where T : class
    {
        public RequestParameters Parameters { get; }
        public IResource<T> Resource { get; }
        public ITarget<T> Target { get; }
        ITarget IRequest.Target => Target;
        private Exception? Error { get; }
        private bool IsEvaluating { get; set; }
        private Headers? _responseHeaders;
        public Headers ResponseHeaders => _responseHeaders ??= new Headers();

        private List<Condition<T>>? _conditions;

        public List<Condition<T>> Conditions
        {
            get => _conditions ??= new List<Condition<T>>();
            set => _conditions = value;
        }

        private MetaConditions? _metaConditions;

        public MetaConditions MetaConditions
        {
            get => _metaConditions ??= new MetaConditions();
            set => _metaConditions = value;
        }

        private Stopwatch Stopwatch { get; }
        public DateTime LogTime { get; } = DateTime.Now;
        private RESTableConfiguration Configuration { get; }

        public Func<IAsyncEnumerable<T>>? Selector { private get; set; }

        public Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>>? Updater { private get; set; }

        public Func<IAsyncEnumerable<T>>? EntitiesProducer { get; set; }

        #region Forwarding properties

        public Cookies Cookies => Context.Client.Cookies;
        public bool HasConditions => !(_conditions?.Count > 0);
        public bool IsValid => Error is null;
        public string ProtocolIdentifier => Parameters.ProtocolIdentifier;
        public TimeSpan TimeElapsed => Stopwatch.Elapsed;
        IEntityResource<T> IEntityRequest<T>.EntityResource => (IEntityResource<T>) Resource;
        public Func<IAsyncEnumerable<T>>? GetCustomSelector() => Selector;
        public Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>>? GetCustomUpdater() => Updater;
        public IContentTypeProvider InputContentTypeProvider => Parameters.InputContentTypeProvider;
        public IContentTypeProvider OutputContentTypeProvider => Parameters.OutputContentTypeProvider;

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
        public IMacro? Macro => Parameters.UriComponents.Macro;
        private ILogable LogItem => Parameters;
        private IHeaderHolder HeaderHolder => Parameters;
        MessageType ILogable.MessageType => LogItem.MessageType;
        ValueTask<string> ILogable.GetLogMessage() => LogItem.GetLogMessage();
        ValueTask<string?> ILogable.GetLogContent() => LogItem.GetLogContent();
        bool IHeaderHolder.ExcludeHeaders => HeaderHolder.ExcludeHeaders;


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

        string? IHeaderHolder.HeadersStringCache
        {
            get => HeaderHolder.HeadersStringCache;
            set => HeaderHolder.HeadersStringCache = value;
        }

        #endregion

        public TData? GetClientData<TData>(string key)
        {
            if (Context.Client.ResourceClientDataMappings.TryGetValue(Resource, out var data) && data!.TryGetValue(key, out var value))
                return (TData?) value;
            return default;
        }

        public void SetClientData<TData>(string key, TData value)
        {
            if (!Context.Client.ResourceClientDataMappings.TryGetValue(Resource, out var data))
                data = Context.Client.ResourceClientDataMappings[Resource] = new ConcurrentDictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            data![key] = value;
        }

        public async ValueTask<IResult> GetResult(CancellationToken cancellationToken = new())
        {
            cancellationToken.ThrowIfCancellationRequested();
            Stopwatch.Restart();
            var result = GetQuickErrorResult();

            if (result is null)
            {
                await Body.Initialize(cancellationToken).ConfigureAwait(false);
                result = await Execute(cancellationToken).ConfigureAwait(false);
            }

            if (Context.HasWaitingWebSocket(out var webSocket) && result is not WebSocketUpgradeSuccessful)
            {
                if (result is Forbidden forbidden)
                    return new WebSocketUpgradeFailed(forbidden);
                await webSocket!.UseOnce(this, async ws =>
                {
                    await ws.SendResult(result, cancellationToken: cancellationToken).ConfigureAwait(false);
                    var message = await ws.GetMessageStream(false, cancellationToken).ConfigureAwait(false);
#if NETSTANDARD2_0
                    using (message)
#else
                    await using (message.ConfigureAwait(false))
#endif
                    {
                        await result.Serialize(message, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                }, cancellationToken).ConfigureAwait(false);
                return new WebSocketTransferSuccess(this);
            }

            if (result is InfiniteLoop loop && !Context.IsBottomOfStack)
                throw loop;

            if (Headers.Metadata == "full" && result.Metadata is string metadata)
                result.Headers.Metadata = metadata;
            result.Headers.Version = Configuration.Version;
            result.Headers.Elapsed = result.TimeElapsed;
            CachedProtocolProvider.ProtocolProvider.SetResultHeaders(result);
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
                        if (Context.HasWaitingWebSocket(out var webSocket))
                        {
                            // Perform WebSocket upgrade, moving from a request context to a WebSocket context.
                            await webSocket!.OpenAndAttachServerSocketToTerminal(this, terminalResource, Conditions, cancellationToken).ConfigureAwait(false);
                            return new WebSocketUpgradeSuccessful(this, webSocket);
                        }
                        return await SwitchTerminal(Context.WebSocket!, terminalResource, cancellationToken).ConfigureAwait(false);
                    }

                    case IBinaryResource<T> binaryResource:
                    {
                        var binaryResult = binaryResource.SelectBinary(this);
                        if (!this.Accepts(binaryResult.ContentType, out var acceptHeader))
                            throw new NotAcceptable(acceptHeader!, binaryResult.ContentType.ToString());
                        var binaryContent = new Binary(this, binaryResult);
                        return binaryContent;
                    }

                    case IEntityResource<T> entityResource:
                    {
                        if (entityResource.RequiresAuthentication)
                        {
                            var authenticator = this.GetRequiredService<ResourceAuthenticator>();
                            await authenticator.ResourceAuthenticate(this, entityResource, cancellationToken).ConfigureAwait(false);
                        }
                        if (MetaConditions.SafePost is not null)
                        {
                            if (!entityResource.CanSelect) throw new SafePostNotSupported("(no selector implemented)");
                            if (!entityResource.CanUpdate) throw new SafePostNotSupported("(no updater implemented)");
                        }
                        var evaluator = EntityOperations<T>.GetMethodEvaluator(Method);
                        var result = await evaluator(this, cancellationToken).ConfigureAwait(false);
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

        private IResult? GetQuickErrorResult()
        {
            if (IsEvaluating)
                throw new InfiniteLoop();
            if (Error is not null)
                return Error.AsResultOf(this);
            if (!Context.MethodIsAllowed(Method, Resource, out var error))
                return error!.AsResultOf(this);
            if (Context.HasWaitingWebSocket(out _))
            {
                try
                {
                    if (!CachedProtocolProvider.ProtocolProvider.IsCompliant(this, out var reason))
                        return new NotCompliantWithProtocol(CachedProtocolProvider.ProtocolProvider, reason!).AsResultOf(this);
                }
                catch (NotImplementedException) { }
            }
            return null;
        }

        /// <summary>
        /// This method is called from a websocket, so the context of this request is already a
        /// WebSocket context. This is what it means to not be a WebSocket upgrade request. 
        /// </summary>
        private async Task<IResult> SwitchTerminal(WebSocket webSocket, ITerminalResource<T> resource, CancellationToken cancellationToken)
        {
            var _resource = (TerminalResource<T>) resource;
            var newTerminal = await _resource.CreateTerminal(Context, cancellationToken, Conditions).ConfigureAwait(false);
            await webSocket.ConnectTo(newTerminal).ConfigureAwait(false);
            await newTerminal.OpenTerminal(cancellationToken).ConfigureAwait(false);
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
                        if (entityResource.ViewDictionary.TryGetValue(parameters.UriComponents.ViewName, out var view) && view is ITarget<T> viewTarget)
                            Target = viewTarget;
                        else throw new UnknownView(parameters.UriComponents.ViewName, entityResource);
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
            Exception? error
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
            Configuration = this.GetRequiredService<RESTableConfiguration>();
        }

        #region Source and destination

        #endregion

        public async ValueTask<IRequest> GetCopy(string? newProtocol = null)
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

        public object? GetService(Type serviceType) => Context.GetService(serviceType);

        public void Dispose() => Body.Dispose();

        public async ValueTask DisposeAsync() => await Body.DisposeAsync().ConfigureAwait(false);
    }
}