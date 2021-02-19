using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
        public Type TargetType => typeof(T);
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
        public bool IsValid => Error == null;
        public string ProtocolIdentifier => Parameters.ProtocolIdentifier;
        public TimeSpan TimeElapsed => Stopwatch.Elapsed;
        private Stopwatch Stopwatch => Parameters.Stopwatch;
        IEntityResource<T> IEntityRequest<T>.EntityResource => Resource as IEntityResource<T>;

        public Func<IAsyncEnumerable<T>> GetSelector() => Selector;
        public Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>> GetUpdater() => Updater;

        public Func<IAsyncEnumerable<T>> Selector { private get; set; }
        public Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>> Updater { private get; set; }

        public Func<IAsyncEnumerable<T>> EntitiesProducer { get; set; }

        public IEnumerable<T> GetInputEntities() => GetInputEntitiesAsync().ToEnumerable();

        public async IAsyncEnumerable<T> GetInputEntitiesAsync()
        {
            if (EntitiesProducer == null) yield break;
            await foreach (var item in EntitiesProducer())
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

        public async Task<IEntities<T>> EvaluateToEntities()
        {
            var result = await Evaluate();
            if (result is Error e) throw e;
            return (IEntities<T>) result;
        }

        public async Task<IResult> Evaluate()
        {
            var sourceDelegate = GetSourceDelegate();
            var destinationDelegate = GetDestinationDelegate();
            var result = GetQuickErrorResult();

            if (result == null)
            {
                Body = await sourceDelegate(Body);
                await Body.Initialize();
                result = await RunEvaluation();
            }

            if (IsWebSocketUpgrade && !(result is WebSocketUpgradeSuccessful))
            {
                await using var webSocket = Context.WebSocket;
                if (result is Forbidden forbidden)
                    return new WebSocketUpgradeFailed(forbidden);
                var serialized = await result.Serialize();
                await Context.WebSocket.Open(this);
                await Context.WebSocket.SendSerializedResult(serialized);
                return new WebSocketTransferSuccess(this);
            }

            result = await destinationDelegate(result);

            result.Headers.Elapsed = TimeElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            if (Headers.Metadata == "full" && result.Metadata is string metadata)
                result.Headers.Metadata = metadata;
            result.Headers.Version = RESTableConfig.Version;
            if (result is InfiniteLoop loop && !Context.IsBottomOfStack)
                throw loop;
            return result;
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
                            await Context.WebSocket.Open(this);
                            await terminal.OpenTerminal();
                            return new WebSocketUpgradeSuccessful(this, Context.WebSocket);
                        }
                        return await SwitchTerminal(terminalResource);

                    case IBinaryResource<T> binary:
                        var (stream, contentType) = await binary.SelectBinary(this);
                        var binaryResult = new Binary(this, contentType);
                        await stream.CopyToAsync(binaryResult.Stream);
                        binaryResult.Stream.Seek(0, SeekOrigin.Begin);
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

        private async Task<IResult> SwitchTerminal(ITerminalResource<T> resource)
        {
            var _resource = (Meta.Internal.TerminalResource<T>) resource;
            var newTerminal = _resource.MakeTerminal(Conditions);
            await Context.WebSocket.ConnectTo(newTerminal, resource);
            await newTerminal.OpenTerminal();
            return new SwitchedTerminal(this);
        }

        internal Request(IResource<T> resource, RequestParameters parameters)
        {
            Parameters = parameters;
            Resource = resource;
            Target = resource;
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
            CachedProtocolProvider cachedProtocolProvider,
            RequestParameters parameters,
            Body body,
            MetaConditions metaConditions,
            List<Condition<T>> conditions,
            Exception error
        )
        {
            Resource = resource;
            Target = target;
            Parameters = parameters;
            Body = body;
            MetaConditions = metaConditions;
            Conditions = conditions;
            Error = error;
            CachedProtocolProvider = cachedProtocolProvider;
        }

        #region Source and destination

        private Func<IResult, Task<IResult>> SendToDestinationDelegate(string destinationHeader)
        {
            if (!HeaderRequestParameters.TryParse(destinationHeader, out var parameters, out var parseError))
            {
                Error = parseError;
                return Task.FromResult;
            }

            if (parameters.IsInternal)
            {
                return async result =>
                {
                    var serializedResult = await result.Serialize();
                    await using var internalRequest = serializedResult.Context.CreateRequest
                    (
                        method: parameters.Method,
                        uri: parameters.URI,
                        body: serializedResult.Body,
                        headers: parameters.Headers
                    );
                    return await internalRequest.Evaluate();
                };
            }
            return async result =>
            {
                var serializedResult = await result.Serialize();
                var externalRequest = new HttpRequest(result, parameters, serializedResult.Body);
                var response = await externalRequest.GetResponseAsync()
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
            if (!HeaderRequestParameters.TryParse(sourceHeader, out var parameters, out var parseError))
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
                if (parameters.IsInternal)
                {
                    await using var internalRequest = Context.CreateRequest
                    (
                        method: parameters.Method,
                        uri: parameters.URI,
                        body: null,
                        headers: parameters.Headers
                    );
                    var result = await internalRequest.Evaluate();
                    if (result is not IEntities)
                        throw new InvalidExternalSource(parameters.URI, await result.GetLogMessage());
                    var serialized = await result.Serialize();
                    if (serialized.Result is Error error) throw error;
                    if (serialized.EntityCount == 0) throw new InvalidExternalSource(parameters.URI, "Response was empty");
                    return serialized.Body;
                }
                parameters.Headers.Accept ??= ContentType.JSON;
                var request = new HttpRequest(this, parameters, null);
                var response = await request.GetResponseAsync() ?? throw new InvalidExternalSource(parameters.URI, "No response");
                if (response.StatusCode >= HttpStatusCode.BadRequest) throw new InvalidExternalSource(parameters.URI, response.LogMessage);
                if (response.Body.CanSeek && response.Body.Length == 0)
                    throw new InvalidExternalSource(parameters.URI, "Response was empty");
                body = new Body(this, response.Body);
                return body;
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

        public async Task<IRequest> GetCopy(string newProtocol = null) => new Request<T>
        (
            resource: Resource,
            target: Target,
            cachedProtocolProvider: newProtocol != null
                ? ProtocolController.ResolveProtocolProvider(newProtocol)
                : CachedProtocolProvider,
            parameters: Parameters,
            body: await Body.GetCopy(),
            metaConditions: MetaConditions.GetCopy(),
            conditions: Conditions.ToList(),
            error: Error
        );

        public object GetService(Type serviceType) => Context.Services.GetService(serviceType);

        public void Dispose() => Body.Dispose();

        public async ValueTask DisposeAsync() => await Body.DisposeAsync();
    }
}