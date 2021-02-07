using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Meta.Internal;
using RESTable.Resources.Operations;
using RESTable.Results;
using RESTable.Internal.Auth;
using RESTable.Linq;
using static RESTable.ErrorCodes;
using static RESTable.Method;

namespace RESTable.Requests
{
    internal class Request<T> : IRequest, IRequest<T>, IEntityRequest<T>, ITraceable where T : class
    {
        public RequestParameters Parameters { get; }
        public IResource<T> Resource { get; }
        public ITarget<T> Target { get; }
        public Type TargetType { get; }
        private Exception Error { get; }
        private bool IsEvaluating { get; set; }
        private Headers _responseHeaders;
        public Headers ResponseHeaders => _responseHeaders ??= new Headers();
        private IDictionary<Type, object> Services { get; }


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
        public bool IsValid => Error == null;
        public string ProtocolIdentifier => Parameters.ProtocolIdentifier;
        public TimeSpan TimeElapsed => Stopwatch.Elapsed;
        private Stopwatch Stopwatch => Parameters.Stopwatch;
        IEntityResource<T> IEntityRequest<T>.EntityResource => Resource as IEntityResource<T>;

        public Func<Task<IEnumerable<T>>> GetSelector() => Selector;
        public Func<IEnumerable<T>, Task<IEnumerable<T>>> GetUpdater() => Updater;

        public Func<Task<IEnumerable<T>>> Selector { private get; set; }
        public Func<IEnumerable<T>, Task<IEnumerable<T>>> Updater { private get; set; }
        public Func<Task<IEnumerable<T>>> EntitiesProducer { get; set; }

        public async Task<IEnumerable<T>> GetInputEntities()
        {
            if (EntitiesProducer != null)
                return await EntitiesProducer();
            return new T[0];
        }

        IResource IRequest.Resource => Resource;
        public Headers Headers => Parameters.Headers;
        public string TraceId => Parameters.TraceId;
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
            metaConditions: MetaConditions.AsConditionList(),
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

        public void EnsureServiceAttached<TService>(TService service) where TService : class
        {
            if (Services.ContainsKey(typeof(TService))) return;
            Services[typeof(TService)] = service;
        }

        public void EnsureServiceAttached<TService, TImplementation>(TImplementation service)
            where TImplementation : class, TService
            where TService : class
        {
            if (Services.ContainsKey(typeof(TService))) return;
            Services[typeof(TService)] = service;
        }

        public object GetService(Type serviceType)
        {
            return Services.TryGetValue(serviceType, out var service) ? service : null;
        }


        public async Task<IEntities<T>> EvaluateToEntities()
        {
            var result = await Evaluate();
            if (result is Error e) throw e;
            return (IEntities<T>) result;
        }

        public async Task<IResult> Evaluate()
        {
            if (Headers.Source is string sourceHeader)
                Body = await GetBodyFromSourceHeader(sourceHeader);

            var result = GetQuickErrorResult() ?? await RunEvaluation();

            if (IsWebSocketUpgrade && !(result is WebSocketUpgradeSuccessful))
            {
                await using var webSocket = Context.WebSocket;
                if (result is Forbidden forbidden)
                    return new WebSocketUpgradeFailed(forbidden);
                var serialized = result.Serialize();
                await Context.WebSocket.Open();
                await Context.WebSocket.SendResult(serialized);
                return new WebSocketTransferSuccess(this);
            }

            result.Headers.Elapsed = TimeElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            if (Headers.Metadata == "full" && result.Metadata is string metadata)
                result.Headers.Metadata = metadata;
            result.Headers.Version = RESTableConfig.Version;
            if (result is InfiniteLoop loop && !Context.IsBottomOfStack)
                throw loop;
            return result;
        }

        private IResult GetQuickErrorResult()
        {
            if (!IsValid) return Error.AsResultOf(this);
            if (!Context.MethodIsAllowed(Method, Resource, out var error)) return error.AsResultOf(this);
            if (IsWebSocketUpgrade)
            {
                try
                {
                    if (!CachedProtocolProvider.ProtocolProvider.IsCompliant(this, out var reason))
                        return new NotCompliantWithProtocol(CachedProtocolProvider.ProtocolProvider, reason).AsResultOf(this);
                }
                catch (NotImplementedException) { }
            }
            if (IsEvaluating) throw new InfiniteLoop();
            return null;
        }

        private async Task<IResult> RunEvaluation()
        {
            try
            {
                Context.IncreaseDepth();
                IsEvaluating = true;

                switch (Resource)
                {
                    case ITerminalResource<T> terminalResource:
                        if (!Context.HasWebSocket)
                            throw new UpgradeRequired(terminalResource.Name);
                        if (IsWebSocketUpgrade)
                        {
                            var terminalResourceInternal = (Meta.Internal.TerminalResource<T>) terminalResource;
                            var terminal = terminalResourceInternal.MakeTerminal(Conditions);
                            Context.WebSocket.SetContext(this);
                            await Context.WebSocket.ConnectTo(terminal, terminalResourceInternal);
                            await Context.WebSocket.Open();
                            await terminal.Open();
                            return new WebSocketUpgradeSuccessful(this, Context.WebSocket);
                        }
                        return await SwitchTerminal(terminalResource);

                    case IBinaryResource<T> binary:
                        var (stream, contentType) = await binary.SelectBinary(this);
                        var binaryResult = new Binary(this, contentType);
                        await stream.CopyToAsync(binaryResult.Body);
                        return binaryResult;

                    case IEntityResource<T> entity:
                        if (entity.RequiresAuthentication)
                            await this.RunResourceAuthentication(entity);
                        if (MetaConditions.SafePost != null)
                        {
                            if (!entity.CanSelect) throw new SafePostNotSupported("(no selector implemented)");
                            if (!entity.CanUpdate) throw new SafePostNotSupported("(no updater implemented)");
                        }
                        var evaluator = EntityOperations<T>.GetMethodEvaluator(Method);
                        var result = await evaluator(this);
                        ResponseHeaders.ForEach(h => result.Headers[h.Key.StartsWith("X-") ? h.Key : "X-" + h.Key] = h.Value);
                        if (RESTableConfig.AllowAllOrigins)
                            result.Headers.AccessControlAllowOrigin = "*";
                        else if (Headers.Origin is string origin)
                            result.Headers.AccessControlAllowOrigin = origin;
                        return result;

                    case var other: throw new UnknownResource(other.Name);
                }
            }
            catch (Exception exception)
            {
                var result = exception.AsResultOf(this);
                ResponseHeaders.ForEach(h => result.Headers[h.Key.StartsWith("X-") ? h.Key : "X-" + h.Key] = h.Value);
                return result;
            }
            finally
            {
                Context.DecreaseDepth();
                IsEvaluating = false;
            }
        }

        private async Task<ISerializedResult> SwitchTerminal(ITerminalResource<T> resource)
        {
            var _resource = (Meta.Internal.TerminalResource<T>) resource;
            var newTerminal = _resource.MakeTerminal(Conditions);
            await Context.WebSocket.ConnectTo(newTerminal, resource);
            await newTerminal.Open();
            return new SwitchedTerminal(this);
        }

        private async Task<Body> GetBodyFromSourceHeader(string sourceHeader)
        {
            try
            {
                Body body;
                var source = new HeaderRequestParameters(sourceHeader);
                if (source.Method != GET) throw new InvalidSyntax(InvalidSource, "Only GET is allowed in Source headers");
                if (source.IsInternal)
                {
                    var result = await Context
                        .CreateRequest(source.URI, source.Method, null, source.Headers)
                        .Evaluate();
                    if (!(result is IEntities)) throw new InvalidExternalSource(source.URI, await result.GetLogMessage());
                    var serialized = result.Serialize();
                    if (serialized is NoContent) throw new InvalidExternalSource(source.URI, "Response was empty");
                    body = new Body(this, serialized.Body);
                }
                else
                {
                    if (source.Headers.Accept == null) source.Headers.Accept = ContentType.JSON;
                    var request = new HttpRequest(this, source, null);
                    var response = await request.GetResponseAsync() ?? throw new InvalidExternalSource(source.URI, "No response");
                    if (response.StatusCode >= HttpStatusCode.BadRequest) throw new InvalidExternalSource(source.URI, response.LogMessage);
                    if (response.Body.CanSeek && response.Body.Length == 0)
                        throw new InvalidExternalSource(source.URI, "Response was empty");
                    body = new Body(this, response.Body);
                }
                return body;
            }
            catch (HttpRequestException re)
            {
                throw new InvalidSyntax(InvalidSource, $"{re.Message} in the Source header");
            }
        }

        internal Request(IResource<T> resource, RequestParameters parameters)
        {
            Parameters = parameters;
            Services = new Dictionary<Type, object>();
            Resource = resource;
            Target = resource;
            TargetType = typeof(T);
            Body = parameters.Body;

            try
            {
                if (resource.IsInternal && Context.Client.Origin != OriginType.Internal)
                    throw new ResourceIsInternal(resource);
                if (Resource is IEntityResource<T> entityResource)
                {
                    MetaConditions = MetaConditions.Parse(parameters.UriComponents.MetaConditions, entityResource);
                    if (parameters.UriComponents.ViewName != null)
                    {
                        if (!entityResource.ViewDictionary.TryGetValue(parameters.UriComponents.ViewName, out var view))
                            throw new UnknownView(parameters.UriComponents.ViewName, entityResource);
                        Target = view;
                    }
                }
                if (parameters.UriComponents.Conditions.Count > 0)
                    Conditions = Condition<T>.Parse(parameters.UriComponents.Conditions, Target);
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
            Type targetType,
            CachedProtocolProvider cachedProtocolProvider,
            RequestParameters parameters,
            Body body,
            MetaConditions metaConditions,
            List<Condition<T>> conditions,
            Exception error
        )
        {
            Services = new Dictionary<Type, object>();
            Resource = resource;
            Target = target;
            TargetType = targetType;
            Parameters = parameters;
            Body = body;
            MetaConditions = metaConditions;
            Conditions = conditions;
            Error = error;
            CachedProtocolProvider = cachedProtocolProvider;
        }

        public async Task<IRequest> GetCopy(string newProtocol = null) => new Request<T>
        (
            resource: Resource,
            target: Target,
            targetType: TargetType,
            cachedProtocolProvider: newProtocol != null
                ? ProtocolController.ResolveProtocolProvider(newProtocol)
                : CachedProtocolProvider,
            parameters: Parameters,
            body: await Body.GetCopy(),
            metaConditions: MetaConditions.GetCopy(),
            conditions: Conditions.ToList(),
            error: Error
        );

        public async ValueTask DisposeAsync()
        {
            await Body.DisposeAsync();
            foreach (var disposable in Services.Values.OfType<IAsyncDisposable>())
                await disposable.DisposeAsync();
            foreach (var disposable in Services.Values.OfType<IDisposable>())
                disposable.Dispose();
        }
    }
}