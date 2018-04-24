using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using RESTar.Logging;
using RESTar.Admin;
using RESTar.Auth;
using RESTar.Internal;
using RESTar.Operations;
using static RESTar.Method;
using static RESTar.Internal.ErrorCodes;
using RESTar.Linq;
using RESTar.Resources;
using RESTar.Results;
using RESTar.Serialization;

namespace RESTar.Requests
{
    internal class Request<T> : IRequest, IRequest<T>, IEntityRequest<T>, ITraceable where T : class
    {
        private bool IsEvaluating { get; set; }
        public ITarget<T> Target { get; }
        public Type TargetType { get; }
        public bool HasConditions => !(_conditions?.Count > 0);

        private Headers _responseHeaders;
        public Headers ResponseHeaders => _responseHeaders ?? (_responseHeaders = new Headers());

        private ICollection<string> _cookies;
        public ICollection<string> Cookies => _cookies ?? (_cookies = new List<string>());

        private Exception Error { get; }
        public bool IsValid => Error == null;
        public Func<IEnumerable<T>> EntitiesProducer { get; set; }
        public Func<IEnumerable<T>> Selector { private get; set; }
        public Func<IEnumerable<T>, IEnumerable<T>> Updater { private get; set; }
        public Func<IEnumerable<T>, IEnumerable<T>> GetUpdater() => Updater;
        public Func<IEnumerable<T>> GetSelector() => Selector;
        public IResource<T> Resource { get; }
        public IEntityResource<T> EntityResource => Resource as IEntityResource<T>;
        IResource IRequest.Resource => Resource;
        public Method Method { get; set; }

        private List<Condition<T>> _conditions;

        public List<Condition<T>> Conditions
        {
            get => _conditions ?? (_conditions = new List<Condition<T>>());
            set => _conditions = value;
        }

        private MetaConditions _metaConditions;

        public MetaConditions MetaConditions
        {
            get => _metaConditions ?? (_metaConditions = new MetaConditions());
            set => _metaConditions = value;
        }

        private Body _body;

        public Body Body
        {
            get => _body;
            private set
            {
                if (IsEvaluating)
                    throw new InvalidOperationException("Cannot set the request body while the request is evaluating");
                _body = value;
            }
        }

        public void SetBody(object content)
        {
            var stream = content != null ? Serializers.Json.SerializeStream(content) : new MemoryStream();
            var contentType = Serializers.Json.ContentType;
            Body = new Body(new RESTarStreamController(stream), contentType, CachedProtocolProvider);
        }

        public void SetBody(byte[] bytes, ContentType? contentType = null)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            var _contentType = contentType ?? Headers.ContentType ?? CachedProtocolProvider.DefaultInputProvider.ContentType;
            Body = new Body(new RESTarStreamController(bytes), _contentType, CachedProtocolProvider);
        }

        public void SetBody(Stream stream, ContentType? contentType = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            var _contentType = contentType ?? Headers.ContentType ?? CachedProtocolProvider.DefaultInputProvider.ContentType;
            Body = new Body(new RESTarStreamController(stream), _contentType, CachedProtocolProvider);
        }

        public IUriComponents UriComponents => new UriComponents
        (
            resourceSpecifier: Resource.Name,
            viewName: Target is IView ? Target.Name : null,
            conditions: Conditions,
            metaConditions: MetaConditions.AsConditionList(),
            protocolProvider: CachedProtocolProvider.ProtocolProvider
        );

        #region Parameter bindings

        public RequestParameters Parameters { get; }

        public string TraceId => Parameters.TraceId;
        public Context Context => Parameters.Context;
        public CachedProtocolProvider CachedProtocolProvider => Parameters.CachedProtocolProvider;
        public Headers Headers => Parameters.Headers;
        public bool IsWebSocketUpgrade => Parameters.IsWebSocketUpgrade;
        public TimeSpan TimeElapsed => Parameters.Stopwatch.Elapsed;

        #endregion

        #region ILogable

        private ILogable LogItem => Parameters;
        LogEventType ILogable.LogEventType => LogItem.LogEventType;
        string ILogable.LogMessage => LogItem.LogMessage;
        string ILogable.LogContent => LogItem.LogContent;
        public DateTime LogTime { get; } = DateTime.Now;

        string ILogable.HeadersStringCache
        {
            get => LogItem.HeadersStringCache;
            set => LogItem.HeadersStringCache = value;
        }

        bool ILogable.ExcludeHeaders => LogItem.ExcludeHeaders;

        #endregion

        public IEntities<T> ResultEntities => (IEntities<T>) Result;

        public IResult Result
        {
            get
            {
                if (!IsValid) return Error.AsResultOf(this);
                if (!MethodCheck(out var _failedAuth))
                    return new MethodNotAllowed(Method, Resource, _failedAuth).AsResultOf(this);
                if (IsWebSocketUpgrade)
                    try
                    {
                        if (!CachedProtocolProvider.ProtocolProvider.IsCompliant(this, out var reason))
                            return new NotCompliantWithProtocol(CachedProtocolProvider.ProtocolProvider, reason).AsResultOf(this);
                    }
                    catch (NotImplementedException) { }
                if (IsEvaluating) throw new InfiniteLoop();
                var result = RunEvaluation();
                result.Headers.Elapsed = TimeElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
                if (Headers.Metadata == "full" && result.Metadata is string metadata)
                    result.Headers.Metadata = metadata;
                result.Headers.Version = RESTarConfig.Version;
                if (result is InfiniteLoop loop && !Context.IsBottomIfStack)
                    throw loop;
                return result;
            }
        }

        private bool MethodCheck(out bool failedAuth)
        {
            if (Method < GET || Method > HEAD)
                throw new ArgumentException($"Invalid method value {Method} for request");
            failedAuth = false;
            if (Resource?.AvailableMethods.Contains(Method) != true) return false;
            if (Context.Client.AccessRights[Resource]?.Contains(Method) == true) return true;
            failedAuth = true;
            return false;
        }

        private IResult RunEvaluation()
        {
            try
            {
                Context.IncreaseDepth();
                IsEvaluating = true;
                switch (Resource)
                {
                    case Resources.TerminalResource<T> terminal:
                        if (!Context.HasWebSocket) return new UpgradeRequired(terminal.Name);
                        if (IsWebSocketUpgrade)
                            return MakeWebSocketUpgrade(terminal);
                        return SwitchTerminal(terminal);

                    case Resources.IBinaryResource<T> binary:
                        var (stream, contentType ) = binary.SelectBinary(this);
                        return new Binary(this, stream, contentType);

                    case IEntityResource<T> entity:
                        this.RunResourceAuthentication(entity);
                        var result = EntityOperations<T>.GetEvaluator(Method).Invoke(this);
                        result.Cookies = Cookies;
                        ResponseHeaders.ForEach(h => result.Headers[h.Key.StartsWith("X-") ? h.Key : "X-" + h.Key] = h.Value);
                        if ((RESTarConfig.AllowAllOrigins ? "*" : Headers.Origin) is string origin)
                            result.Headers["Access-Control-Allow-Origin"] = origin;
                        if (!IsWebSocketUpgrade) return result;
                        var serialized = result.Serialize();
                        Context.WebSocket.SendResult(serialized);
                        Context.WebSocket.Disconnect();
                        return new WebSocketUpgradeSuccessful(this);

                    case var other: throw new UnknownResource(other.Name);
                }
            }
            catch (Exception exception)
            {
                return exception.AsResultOf(this);
            }
            finally
            {
                Context.DecreaseDepth();
                IsEvaluating = false;
            }
        }

        private ISerializedResult SwitchTerminal(Resources.TerminalResource<T> resource)
        {
            var newTerminal = resource.MakeTerminal(Conditions);
            Context.WebSocket.ConnectTo(newTerminal, resource);
            newTerminal.Open();
            return new SwitchedTerminal(this);
        }

        private ISerializedResult MakeWebSocketUpgrade(Resources.TerminalResource<T> resource)
        {
            var terminal = resource.MakeTerminal(Conditions);
            Context.WebSocket.SetContext(this);
            Context.WebSocket.ConnectTo(terminal, resource);
            Context.WebSocket.Open();
            terminal.Open();
            return new WebSocketUpgradeSuccessful(this);
        }

        public IEnumerable<T> GetInputEntities() => EntitiesProducer?.Invoke() ?? new T[0];

        internal Request(IResource<T> resource, RequestParameters parameters)
        {
            Parameters = parameters;
            Resource = resource;
            Target = resource;
            TargetType = typeof(T);
            Method = parameters.Method;

            try
            {
                if (resource.IsInternal && Context.Client.Origin != OriginType.Internal)
                    throw new ResourceIsInternal(resource);
                if (Resource is IEntityResource<T> entityResource)
                {
                    MetaConditions = MetaConditions.Parse(parameters.Uri.MetaConditions, entityResource);
                    if (parameters.Uri.ViewName != null)
                    {
                        if (!entityResource.ViewDictionary.TryGetValue(parameters.Uri.ViewName, out var view))
                            throw new UnknownView(parameters.Uri.ViewName, entityResource);
                        Target = view;
                    }
                }
                if (parameters.Uri.Conditions.Count > 0)
                    Conditions = Condition<T>.Parse(parameters.Uri.Conditions, Target);
                if (parameters.Headers.UnsafeOverride)
                {
                    MetaConditions.Unsafe = true;
                    parameters.Headers.UnsafeOverride = false;
                }
                if (Context.Client.Origin == OriginType.Internal && Method == GET)
                    MetaConditions.Formatter = DbOutputFormat.Raw;
                var defaultContentType = CachedProtocolProvider.DefaultInputProvider.ContentType;
                if (Headers.Source == null)
                {
                    if (!Parameters.HasBody) return;
                    Body = new Body
                    (
                        stream: new RESTarStreamController(Parameters.BodyBytes),
                        contentType: Headers.ContentType ?? defaultContentType,
                        protocolProvider: CachedProtocolProvider
                    );
                }
                else
                {
                    try
                    {
                        var source = new HeaderRequestParameters(Headers.Source);
                        if (source.Method != GET) throw new InvalidSyntax(InvalidSource, "Only GET is allowed in Source headers");
                        if (source.IsInternal)
                        {
                            var result = Context.CreateRequest(source.Method, source.URI, null, source.Headers).Result;
                            if (!(result is IEntities)) throw new InvalidExternalSource(source.URI, result.LogMessage);
                            var serialized = result.Serialize();
                            if (serialized is NoContent) throw new InvalidExternalSource(source.URI, "Response was empty");
                            Body = new Body
                            (
                                stream: new RESTarStreamController(serialized.Body),
                                contentType: serialized.Headers.ContentType ?? CachedProtocolProvider.DefaultInputProvider.ContentType,
                                protocolProvider: CachedProtocolProvider
                            );
                        }
                        else
                        {
                            if (source.Headers.Accept == null)
                                source.Headers.Accept = defaultContentType;
                            var request = new HttpRequest(this, source, null);
                            var response = request.GetResponse() ?? throw new InvalidExternalSource(source.URI, "No response");
                            if (response.StatusCode >= HttpStatusCode.BadRequest)
                                throw new InvalidExternalSource(source.URI, response.LogMessage);
                            if (response.Body.CanSeek && response.Body.Length == 0)
                                throw new InvalidExternalSource(source.URI, "Response was empty");
                            Body = new Body(new RESTarStreamController(response.Body), response.Headers.ContentType ?? defaultContentType,
                                CachedProtocolProvider);
                        }
                    }
                    catch (HttpRequestException re)
                    {
                        throw new InvalidSyntax(InvalidSource, $"{re.Message} in the Source header");
                    }
                }
            }
            catch (Exception e)
            {
                Error = e;
            }
        }

        public void Dispose() => Body.Dispose();
    }
}