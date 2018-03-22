using System;
using System.Collections.Generic;
using System.Linq;
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
    internal class ParsedRequest<T> : IRequest<T>, IRequestInternal<T>, ITraceable where T : class
    {
        public Func<IEnumerable<T>> EntitiesGenerator { private get; set; }
        public ITarget<T> Target { get; }

        public Condition<T>[] Conditions
        {
            get => _conditions;
            set
            {
                if (IsEvaluating)
                    throw new InvalidOperationException(
                        "Cannot set resource conditions while the request is evaluating");
                _conditions = value;
            }
        }

        public MetaConditions MetaConditions { get; }

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

        public Headers ResponseHeaders { get; }
        public ICollection<string> Cookies { get; }
        public string Source { get; }
        public string Destination { get; }

        #region Parameter bindings

        public RequestParameters RequestParameters { get; }
        public Methods Method => RequestParameters.Method;
        public string TraceId => RequestParameters.TraceId;
        public Client Client => RequestParameters.Client;

        public IUriComponents UriComponents
        {
            get
            {
                // return RequestParameters.Uri;
            }
        }

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

        #region Private and explicit

        IEntityResource IRequest.Resource => EntityResource;
        IEntityResource<T> IRequest<T>.Resource => EntityResource;
        private IEntityResource<T> EntityResource => IResource as IEntityResource<T>;
        private ITerminalResourceInternal TerminalResource => IResource as ITerminalResourceInternal;
        private IResource<T> IResource { get; }

        private string CORSOrigin { get; }
        private DataConfig InputDataConfig { get; }
        private DataConfig OutputDataConfig { get; }
        private bool IsEvaluating;
        private int StackDepth;
        private Body _body;
        private Condition<T>[] _conditions;

        #endregion

        private Exception Error { get; }

        public bool IsValid => Error == null;

        public IResult GetResult()
        {
            if (!IsValid)
                return RESTarError.GetResult(Error, this);
            if (IsEvaluating || StackDepth > 300) throw new InfiniteLoop();
            var result = RunEvaluation();
            if (result is InfiniteLoop loop) throw loop;
            return result;
        }

        private void ResolveContentTypes()
        {
            // Gör det så sent det går. Man ska kunna köra request utan att ange content types om de inte behövs

            if (cachedProtocolProvider != null)
            {
                if (Headers.ContentType == null)
                    Headers.ContentType = cachedProtocolProvider.DefaultInputProvider.ContentType;
                if (Headers.Accept == null)
                    Headers.Accept = new[] {cachedProtocolProvider.DefaultOutputProvider.ContentType};
                else
                {
                    if (!cachedProtocolProvider.InputMimeBindings.TryGetValue(Headers.ContentType.MimeType, out var provider))
                        Error = new UnsupportedContent(Headers["Content-Type"]);
                    else InputContentTypeProvider = provider;
                }

                if (Headers.Accept == null)
                    OutputContentTypeProvider = cachedProtocolProvider.DefaultOutputProvider;
                else
                {
                    IContentTypeProvider acceptProvider = null;
                    var containedWildcard = false;
                    var foundProvider = Headers.Accept.Any(a =>
                    {
                        if (a.MimeType != "*/*")
                            return cachedProtocolProvider.OutputMimeBindings.TryGetValue(a.MimeType, out acceptProvider);
                        containedWildcard = true;
                        return false;
                    });
                    if (!foundProvider)
                        if (containedWildcard)
                            OutputContentTypeProvider = cachedProtocolProvider.DefaultOutputProvider;
                        else
                            Error = new NotAcceptable(Headers["Accept"]);
                    else OutputContentTypeProvider = acceptProvider;
                }
            }
        }

        private IResult RunEvaluation()
        {
            StackDepth += 1;
            IsEvaluating = true;
            try
            {
                switch (IResource)
                {
                    case ITerminalResourceInternal<T> terminal:
                        if (!Client.HasWebSocket)
                            return new UpgradeRequired(terminal.Name);
                        terminal.InstantiateFor(Client.WebSocketInternal, Conditions);
                        return new WebSocketResult(leaveOpen: true, trace: this);
                    case IEntityResource<T> _:
                        this.RunResourceAuthentication();
                        var result = Operations<T>.REST.GetEvaluator(Method)(this);
                        result.Cookies = Cookies;
                        ResponseHeaders.ForEach(h => result.Headers[h.Key.StartsWith("X-") ? h.Key : "X-" + h.Key] = h.Value);
                        if ((RESTarConfig.AllowAllOrigins ? "*" : CORSOrigin) is string allowedOrigin)
                            result.Headers["Access-Control-Allow-Origin"] = allowedOrigin;
                        if (!IsWebSocketUpgrade) return result;
                        var finalized = result.FinalizeResult();
                        Client.WebSocket.SendResult(finalized);
                        return new WebSocketResult(leaveOpen: false, trace: this);
                    default: throw new UnknownResource(IResource.Name);
                }
            }
            catch (Exception exs)
            {
                return RESTarError.GetResult(exs, this);
            }
            finally
            {
                StackDepth -= 1;
                IsEvaluating = false;
            }
        }

        public IEnumerable<T> GetEntities() => EntitiesGenerator?.Invoke() ?? new T[0];

        private ParsedRequest(IResource<T> resource, RequestParameters requestParameters)
        {
            RequestParameters = requestParameters;
            IResource = resource;
            Target = resource;
            ResponseHeaders = new Headers();
            Cookies = new List<string>();
            Conditions = new Condition<T>[0];
            MetaConditions = new MetaConditions();
            Source = requestParameters.Headers.SafeGet("Source");
            Destination = requestParameters.Headers.SafeGet("Destination");
            CORSOrigin = requestParameters.Headers.SafeGet("Origin");
            InputDataConfig = Source != null ? DataConfig.External : DataConfig.Client;
            OutputDataConfig = Destination != null ? DataConfig.External : DataConfig.Client;


            try
            {
                if (resource.IsInternal && !requestParameters.Client.IsInternal)
                    throw new ResourceIsInternal(resource);
                if (requestParameters.Uri.ViewName != null && EntityResource != null)
                {
                    if (!EntityResource.ViewDictionary.TryGetValue(requestParameters.Uri.ViewName, out var view))
                        throw new UnknownView(requestParameters.Uri.ViewName, EntityResource);
                    Target = view;
                }

                Conditions = Condition<T>.Parse(requestParameters.Uri.Conditions, Target) ?? Conditions;
                if (EntityResource != null)
                    MetaConditions = MetaConditions.Parse(requestParameters.Uri.MetaConditions, EntityResource) ?? MetaConditions;
                if (requestParameters.Headers.UnsafeOverride)
                {
                    MetaConditions.Unsafe = true;
                    requestParameters.Headers.UnsafeOverride = false;
                }
                if (Client.IsInternal) MetaConditions.Formatter = DbOutputFormat.Raw;
                this.MethodCheck();
                switch (InputDataConfig)
                {
                    case DataConfig.Client:
                        if (!RequestParameters.Body.HasContent)
                        {
                            if (Method == PATCH || Method == POST || Method == PUT)
                                throw new InvalidSyntax(NoDataSource, "Missing data source for method " + Method);
                            return;
                        }
                        Body = RequestParameters.Body;
                        break;
                    case DataConfig.External:
                        try
                        {
                            var request = new HttpRequest(this, Source) {Accept = RequestParameters.InputContentTypeProvider.ContentType.ToString()};
                            if (request.Method != GET)
                                throw new InvalidSyntax(InvalidSource, "Only GET is allowed in Source headers");
                            var response = request.GetResponse() ?? throw new InvalidExternalSource(request, "No response");
                            if (response.StatusCode >= HttpStatusCode.BadRequest)
                                throw new InvalidExternalSource(request,
                                    $"Status: {response.StatusCode.ToCode()} - {response.StatusDescription}. {response.Headers.SafeGet("RESTar-info")}");
                            if (response.Body.CanSeek && response.Body.Length == 0)
                                throw new InvalidExternalSource(request, "Response was empty");
                            Body = new Body(response.Body.ToByteArray(), RequestParameters.InputContentTypeProvider);
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

        internal static IRequest<T> Create(IResource<T> resource, RequestParameters requestParameters) =>
            new ParsedRequest<T>(resource, requestParameters);
    }
}