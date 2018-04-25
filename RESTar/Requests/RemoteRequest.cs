using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Logging;
using RESTar.Resources;
using RESTar.Results;

namespace RESTar.Requests
{
    internal class RemoteRequest : IRequest, IRequestInternal
    {
        private static readonly HttpClient HttpClient;
        static RemoteRequest() => HttpClient = new HttpClient();

        private Uri URI { get; }
        public string TraceId { get; }
        public Context Context { get; }
        public Headers Headers { get; }
        public Method Method { get; set; }
        public Body Body { get; private set; }
        private RemoteResource RemoteResource { get; set; }

        private static string ErrorMessage(string propertyName) => $"Cannot get {propertyName} for a remote request";

        public void SetBody(object content, ContentType? contentType = null) => Body = new Body
        (
            stream: this.GetBodyStream(content, contentType),
            contentType: Headers.ContentType ?? CachedProtocolProvider.DefaultInputProvider.ContentType,
            protocolProvider: CachedProtocolProvider
        );

        public IResource Resource => RemoteResource;
        public Type TargetType => null;
        public bool HasConditions => false;
        private MetaConditions _metaConditions;
        public MetaConditions MetaConditions => _metaConditions ?? (_metaConditions = new MetaConditions());
        private Headers _responseHeaders;
        public Headers ResponseHeaders => _responseHeaders ?? (_responseHeaders = new Headers());
        private ICollection<string> _cookies;
        public ICollection<string> Cookies => _cookies ?? (_cookies = new List<string>());
        public IUriComponents UriComponents => null;

        public IResult Result => GetResult().Result;

        private async Task<IResult> GetResult()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var method = new HttpMethod(Method.ToString());
                var message = new HttpRequestMessage(method, URI);
                switch (Method)
                {
                    case Method.POST:
                    case Method.PATCH:
                    case Method.PUT:
                        if (Body.HasContent)
                        {
                            message.Content = new StreamContent(Body.Stream);
                            message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(Headers.ContentType.ToString());
                        }
                        break;
                }
                Headers.Metadata = "full";
                Headers.ContentType = null;
                Headers.ForEach(header => message.Headers.Add(header.Key, header.Value));
                var response = await HttpClient.SendAsync(message);
                var responseHeaders = new Headers();
                response.Headers.ForEach(header => responseHeaders[header.Key] = header.Value.FirstOrDefault());
                var metadata = responseHeaders.Metadata?.Split(';');
                if (metadata?.Length != 3)
                    return new ExternalServiceNotRESTar(URI).AsResultOf(this);
                var (resultType, resourceName, data) = (metadata[0], metadata[1], metadata[2]);
                if (string.IsNullOrWhiteSpace(resultType))
                    return new ExternalServiceNotRESTar(URI).AsResultOf(this);
                RemoteResource = new RemoteResource(resourceName);
                var stream = default(RESTarStream);
                if (response.Content != null)
                {
                    var streamController = new RESTarStream();
                    await response.Content.CopyToAsync(streamController);
                    stream = streamController.Rewind();
                }

                IResult getResult()
                {
                    int nr;
                    ErrorCodes ec;
                    switch (resultType)
                    {
                        case nameof(Entities<object>) when ulong.TryParse(responseHeaders.EntityCount, out var count):
                            return new RemoteEntities(this, stream, count);
                        case nameof(Head) when int.TryParse(data, out nr): return new Head(this, nr);
                        case nameof(Report) when int.TryParse(data, out nr): return new Report(this, nr);
                        case nameof(Binary): return new Binary(this, stream, responseHeaders.ContentType ?? ContentType.DefaultOutput);
                        case nameof(NoContent): return new NoContent(this);
                        case nameof(InsertedEntities) when int.TryParse(data, out nr): return new InsertedEntities(this, nr);
                        case nameof(UpdatedEntities) when int.TryParse(data, out nr): return new UpdatedEntities(this, nr);
                        case nameof(DeletedEntities) when int.TryParse(data, out nr): return new DeletedEntities(this, nr);
                        case nameof(SafePostedEntities)
                            when data.TSplit(',') is var vt && int.TryParse(vt.Item1, out var upd) && int.TryParse(vt.Item2, out var ins):
                            return new SafePostedEntities(this, upd, ins);

                        case nameof(BadRequest) when Enum.TryParse(data, out ec): return new RemoteBadRequest(ec);
                        case nameof(NotFound) when Enum.TryParse(data, out ec): return new RemoteNotFound(ec);
                        case nameof(Forbidden) when Enum.TryParse(data, out ec): return new RemoteForbidden(ec);
                        case nameof(Results.Internal) when Enum.TryParse(data, out ec): return new RemoteInternal(ec);

                        case nameof(FeatureNotImplemented): return new FeatureNotImplemented(null);
                        case nameof(InfiniteLoop): return new InfiniteLoop();
                        case nameof(MethodNotAllowed): return new MethodNotAllowed(Method, Resource, false);
                        case nameof(NotAcceptable): return new NotAcceptable(Headers.Accept.ToString());
                        case nameof(UnsupportedContent): return new UnsupportedContent(Headers.ContentType.ToString());
                        case nameof(UpgradeRequired): return new UpgradeRequired(Resource.Name);

                        default: return new RemoteOther(this, response.StatusCode, response.ReasonPhrase);
                    }
                }

                var result = getResult();
                if (result is Error error)
                    result = error.AsResultOf(this);
                responseHeaders.ForEach(result.Headers.Put);
                TimeElapsed = sw.Elapsed;
                return result;
            }
            catch (Exception e)
            {
                return new ExternalServiceNotRESTar(URI, e).AsResultOf(this);
            }
        }

        public bool IsValid { get; }
        public TimeSpan TimeElapsed { get; private set; }
        public bool IsWebSocketUpgrade { get; }
        public CachedProtocolProvider CachedProtocolProvider { get; }

        public LogEventType LogEventType { get; }
        public string LogMessage => $"{Method} {URI}{Body.LengthLogString}";
        public string LogContent => Body.ToString();
        public string HeadersStringCache { get; set; }
        public bool ExcludeHeaders { get; }
        public DateTime LogTime { get; }

        public RemoteRequest(RemoteContext context, Method method, string uri, byte[] body, Headers headers)
        {
            TraceId = context.InitialTraceId;
            Context = context;
            Headers = headers ?? new Headers();
            if (context.HasApiKey)
                Headers.Authorization = $"apikey {context.ApiKey}";
            Method = method;
            LogEventType = LogEventType.HttpInput;
            IsValid = true;
            IsWebSocketUpgrade = false;
            URI = new Uri(context.ServiceRoot + uri);
            LogTime = DateTime.Now;
            CachedProtocolProvider = ProtocolController.DefaultProtocolProvider;
            ExcludeHeaders = false;
            if (body?.Length > 0)
                SetBody(body, Headers.ContentType);
        }

        public void Dispose() => Body.Dispose();
    }
}