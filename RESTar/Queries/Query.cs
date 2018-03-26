using System;
using System.Collections.Generic;
using System.Net;
using RESTar.Logging;
using RESTar.Admin;
using RESTar.Auth;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Results.Error;
using RESTar.Results.Error.BadRequest;
using RESTar.Results.Error.Forbidden;
using RESTar.Results.Error.NotFound;
using static RESTar.Method;
using static RESTar.Internal.ErrorCodes;
using RESTar.Linq;
using RESTar.Resources;
using RESTar.Results.Success;

namespace RESTar.Queries
{
    internal class Query<T> : IQuery<T>, IQueryInternal<T>, ITraceable where T : class
    {
        public ITarget<T> Target { get; }
        public bool HasConditions => !(_conditions?.Count > 0);
        public Headers ResponseHeaders => _responseHeaders ?? (_responseHeaders = new Headers());
        public ICollection<string> Cookies => _cookies ?? (_cookies = new List<string>());
        private Exception Error { get; }
        public bool IsValid => Error == null;

        public EntitiesSelector<T> EntitiesProducer { private get; set; }
        public EntitiesSelector<T> Selector { private get; set; }
        public EntitiesUpdater<T> Updater { private get; set; }
        public EntitiesUpdater<T> GetUpdater() => Updater;
        public EntitiesSelector<T> GetSelector() => Selector;

        public Method Method
        {
            get => _method;
            set
            {
                var previous = _method;
                _method = value;
                try
                {
                    this.MethodCheck();
                }
                catch
                {
                    _method = previous;
                    throw;
                }
            }
        }

        public List<Condition<T>> Conditions
        {
            get => _conditions ?? (_conditions = new List<Condition<T>>());
            set => _conditions = value;
        }

        public MetaConditions MetaConditions
        {
            get => _metaConditions ?? (_metaConditions = new MetaConditions());
            set => _metaConditions = value;
        }

        public Body Body
        {
            get => _body;
            set
            {
                if (IsEvaluating)
                    throw new InvalidOperationException(
                        "Cannot set the request body while the request is evaluating");
                _body = value;
            }
        }

        public IUriComponents UriComponents => new UriComponents
        (
            resourceSpecifier: IResource.Name,
            viewName: Target is IView ? Target.Name : null,
            conditions: Conditions,
            metaConditions: MetaConditions.AsConditionList(),
            protocolProvider: CachedProtocolProvider.ProtocolProvider
        );

        #region Private

        private List<Condition<T>> _conditions;
        private MetaConditions _metaConditions;
        private Headers _responseHeaders;
        private ICollection<string> _cookies;
        private Body _body;
        private IResource<T> IResource { get; }
        IEntityResource IQuery.Resource => IResource as IEntityResource;
        IEntityResource<T> IQuery<T>.Resource => IResource as IEntityResource<T>;
        private DataConfig InputDataConfig { get; }
        private DataConfig OutputDataConfig { get; }
        private bool IsEvaluating;
        private Method _method;

        #endregion

        #region Parameter bindings

        public QueryParameters QueryParameters { get; }

        public string TraceId => QueryParameters.TraceId;
        public Context Context => QueryParameters.Context;
        public CachedProtocolProvider CachedProtocolProvider => QueryParameters.CachedProtocolProvider;
        public Headers Headers => QueryParameters.Headers;
        public bool IsWebSocketUpgrade => QueryParameters.IsWebSocketUpgrade;
        public TimeSpan TimeElapsed => QueryParameters.Stopwatch.Elapsed;

        #endregion

        #region ILogable

        private ILogable LogItem => QueryParameters;
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

        public IResult Result
        {
            get
            {
                if (!IsValid) return RESTarError.GetResult(Error, this);
                if (IsWebSocketUpgrade)
                    try
                    {
                        if (!CachedProtocolProvider.ProtocolProvider.IsCompliant(this, out var reason))
                            return RESTarError.GetResult(new NotCompliantWithProtocol(CachedProtocolProvider.ProtocolProvider, reason), this);
                    }
                    catch (NotImplementedException) { }
                if (IsEvaluating) throw new InfiniteLoop();
                var result = RunEvaluation();
                if (result is InfiniteLoop loop) throw loop;
                return result;
            }
        }

        private IResult RunEvaluation()
        {
            try
            {
                Context.IncreaseDepth();
                IsEvaluating = true;
                switch (IResource)
                {
                    case Resources.TerminalResource<T> terminal:
                        if (!Context.HasWebSocket) return new UpgradeRequired(terminal.Name);
                        if (IsWebSocketUpgrade)
                            return MakeWebSocketUpgrade(terminal);
                        return SwitchTerminal(terminal);
                    case IEntityResource<T> _:
                        this.RunResourceAuthentication();
                        var result = Operations<T>.GetEvaluator(Method)(this);
                        result.Cookies = Cookies;
                        ResponseHeaders.ForEach(h => result.Headers[h.Key.StartsWith("X-") ? h.Key : "X-" + h.Key] = h.Value);
                        if ((RESTarConfig.AllowAllOrigins ? "*" : Headers.Origin) is string origin)
                            result.Headers["Access-Control-Allow-Origin"] = origin;
                        if (!IsWebSocketUpgrade) return result;
                        var serialized = result.Serialize();
                        Context.WebSocket.SendResult(serialized);
                        Context.WebSocket.Disconnect();
                        return new WebSocketUpgradeSuccessful(this);
                    default: throw new UnknownResource(IResource.Name);
                }
            }
            catch (Exception exs)
            {
                return RESTarError.GetResult(exs, this);
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

        public IEnumerable<T> GetEntities() => EntitiesProducer?.Invoke() ?? new T[0];

        internal Query(IResource<T> resource, QueryParameters queryParameters)
        {
            QueryParameters = queryParameters;
            IResource = resource;
            Target = resource;
            InputDataConfig = Headers.Source != null ? DataConfig.External : DataConfig.Client;
            OutputDataConfig = Headers.Destination != null ? DataConfig.External : DataConfig.Client;
            
            try
            {
                if (resource.IsInternal && Context.Client.Origin != OriginType.Internal)
                    throw new ResourceIsInternal(resource);
                if (IResource is IEntityResource<T> entityResource)
                {
                    Method = queryParameters.Method;
                    MetaConditions = MetaConditions.Parse(queryParameters.Uri.MetaConditions, entityResource);
                    if (queryParameters.Uri.ViewName != null)
                    {
                        if (!entityResource.ViewDictionary.TryGetValue(queryParameters.Uri.ViewName, out var view))
                            throw new UnknownView(queryParameters.Uri.ViewName, entityResource);
                        Target = view;
                    }
                }
                if (queryParameters.Uri.Conditions.Count > 0)
                    Conditions = Condition<T>.Parse(queryParameters.Uri.Conditions, Target);
                if (queryParameters.Headers.UnsafeOverride)
                {
                    MetaConditions.Unsafe = true;
                    queryParameters.Headers.UnsafeOverride = false;
                }
                if (Context.Client.Origin == OriginType.Internal && Method == GET)
                    MetaConditions.Formatter = DbOutputFormat.Raw;
                var defaultContentType = CachedProtocolProvider.DefaultInputProvider.ContentType;
                switch (InputDataConfig)
                {
                    case DataConfig.Client:
                        if (!QueryParameters.HasBody)
                            return;
                        Body = new Body(QueryParameters.BodyBytes, Headers.ContentType ?? defaultContentType, CachedProtocolProvider);
                        break;
                    case DataConfig.External:
                        try
                        {
                            var request = new HttpRequest(this, Headers.Source)
                            {
                                Accept = Headers.ContentType?.ToString()
                                         ?? defaultContentType.ToString()
                            };
                            if (request.Method != GET)
                                throw new InvalidSyntax(InvalidSource, "Only GET is allowed in Source headers");
                            var response = request.GetResponse() ?? throw new InvalidExternalSource(request, "No response");
                            if (response.StatusCode >= HttpStatusCode.BadRequest)
                                throw new InvalidExternalSource(request,
                                    $"Status: {response.StatusCode.ToCode()} - {response.StatusDescription}. {response.Headers.SafeGet("RESTar-info")}");
                            if (response.Body.CanSeek && response.Body.Length == 0)
                                throw new InvalidExternalSource(request, "Response was empty");
                            Body = new Body(response.Body.ToByteArray(), Headers.ContentType ?? defaultContentType, CachedProtocolProvider);
                            break;
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
    }
}