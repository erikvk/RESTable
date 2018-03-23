using System;
using System.Collections.Generic;
using System.Net;
using RESTar.Logging;
using RESTar.Admin;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Results.Error;
using RESTar.Results.Error.BadRequest;
using RESTar.Results.Error.Forbidden;
using RESTar.Results.Error.NotFound;
using static RESTar.Methods;
using static RESTar.Internal.ErrorCodes;
using RESTar.Http;
using RESTar.Linq;
using RESTar.Results.Success;

namespace RESTar.Requests
{
    internal class Request<T> : IRequest<T>, IRequestInternal<T>, ITraceable where T : class
    {
        private IEntityResource<T> EntityResource => _iresource as IEntityResource<T>;
        private ITerminalResourceInternal TerminalResource => _iresource as ITerminalResourceInternal;
        public ITarget<T> Target { get; }
        public bool HasConditions => !(_conditions?.Count > 0);
        public Headers ResponseHeaders => _responseHeaders ?? (_responseHeaders = new Headers());
        public ICollection<string> Cookies => _cookies ?? (_cookies = new List<string>());
        private Exception Error { get; }
        public bool IsValid => Error == null;

        public EntitiesInserter<T> EntitiesGenerator { private get; set; }
        public EntitiesInserter<T> Inserter { private get; set; }
        public EntitiesUpdater<T> Updater { private get; set; }
        public EntitiesInserter<T> GetInserter() => Inserter ?? (() => Body.ToList<T>());
        public EntitiesUpdater<T> GetUpdater() => Updater ?? (source => Body.PopulateTo(source));

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

        public IUriComponents UriComponents
        {
            get
            {
                var viewName = Target is IView ? Target.Name : null;
                return new UriComponents(_iresource.Name, viewName, Conditions, MetaConditions.AsConditionList());
            }
        }

        #region Private

        private List<Condition<T>> _conditions;
        private MetaConditions _metaConditions;
        private Headers _responseHeaders;
        private ICollection<string> _cookies;
        private Body _body;
        private IResource<T> _iresource { get; }
        IEntityResource IRequest.Resource => EntityResource;
        IEntityResource<T> IRequest<T>.Resource => EntityResource;
        private DataConfig InputDataConfig { get; }
        private DataConfig OutputDataConfig { get; }
        private bool IsEvaluating;

        #endregion

        #region Parameter bindings

        public RequestParameters RequestParameters { get; }
        public Methods Method => RequestParameters.Method;
        public string TraceId => RequestParameters.TraceId;
        public Context Context => RequestParameters.Context;
        public CachedProtocolProvider ProtocolProvider => RequestParameters.CachedProtocolProvider;
        public Headers Headers => RequestParameters.Headers;
        public bool IsWebSocketUpgrade => RequestParameters.IsWebSocketUpgrade;

        #endregion

        #region ILogable

        private ILogable LogItem => RequestParameters;
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

        public IResult GetResult()
        {
            if (!IsValid) return RESTarError.GetResult(Error, this);
            try
            {
                if (!ProtocolProvider.ProtocolProvider.IsCompliant(this, out var reason))
                    return RESTarError.GetResult(new NotCompliantWithProtocol(ProtocolProvider.ProtocolProvider, reason), this);
            }
            catch (NotImplementedException) { }
            if (IsEvaluating) throw new InfiniteLoop();

            var result = RunEvaluation();
            if (result is InfiniteLoop loop) throw loop;
            return result;
        }

        private IResult RunEvaluation()
        {
            try
            {
                Context.IncreaseDepth();
                IsEvaluating = true;
                switch (_iresource)
                {
                    case ITerminalResourceInternal<T> terminal:
                        if (!Context.HasWebSocket)
                            return new UpgradeRequired(terminal.Name);
                        terminal.InstantiateFor(Context.WebSocket, this);
                        return new WebSocketResult(leaveOpen: true, trace: this);
                    case IEntityResource<T> _:
                        this.RunResourceAuthentication();
                        var result = Operations<T>.REST.GetEvaluator(Method)(this);
                        result.Cookies = Cookies;
                        ResponseHeaders.ForEach(h => result.Headers[h.Key.StartsWith("X-") ? h.Key : "X-" + h.Key] = h.Value);
                        if ((RESTarConfig.AllowAllOrigins ? "*" : Headers.Origin) is string allowedOrigin)
                            result.Headers["Access-Control-Allow-Origin"] = allowedOrigin;
                        if (!IsWebSocketUpgrade) return result;
                        var finalized = result.FinalizeResult();
                        Context.WebSocket.SendResult(finalized);
                        return new WebSocketResult(leaveOpen: false, trace: this);
                    default: throw new UnknownResource(_iresource.Name);
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

        public IEnumerable<T> GetEntities() => EntitiesGenerator?.Invoke() ?? new T[0];

        internal Request(IResource<T> resource, RequestParameters requestParameters)
        {
            RequestParameters = requestParameters;
            _iresource = resource;
            Target = resource;
            InputDataConfig = Headers.Source != null ? DataConfig.External : DataConfig.Client;
            OutputDataConfig = Headers.Destination != null ? DataConfig.External : DataConfig.Client;

            try
            {
                if (resource.IsInternal && Context.Client.Origin != OriginType.Internal)
                    throw new ResourceIsInternal(resource);
                if (requestParameters.Uri.ViewName != null && EntityResource != null)
                {
                    if (!EntityResource.ViewDictionary.TryGetValue(requestParameters.Uri.ViewName, out var view))
                        throw new UnknownView(requestParameters.Uri.ViewName, EntityResource);
                    Target = view;
                }
                if (requestParameters.Uri.Conditions.Count > 0)
                    Conditions = Condition<T>.Parse(requestParameters.Uri.Conditions, Target);
                if (EntityResource != null)
                    MetaConditions = MetaConditions.Parse(requestParameters.Uri.MetaConditions, EntityResource);
                if (requestParameters.Headers.UnsafeOverride)
                {
                    MetaConditions.Unsafe = true;
                    requestParameters.Headers.UnsafeOverride = false;
                }
                if (Context.Client.Origin == OriginType.Internal && Method == GET)
                    MetaConditions.Formatter = DbOutputFormat.Raw;
                this.MethodCheck();
                var defaultContentType = ProtocolProvider.DefaultInputProvider.ContentType;
                switch (InputDataConfig)
                {
                    case DataConfig.Client:
                        if (!RequestParameters.HasBody)
                            return;
                        Body = new Body(RequestParameters.BodyBytes, Headers.ContentType ?? defaultContentType, ProtocolProvider);
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
                            Body = new Body(response.Body.ToByteArray(), Headers.ContentType ?? defaultContentType, ProtocolProvider);
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