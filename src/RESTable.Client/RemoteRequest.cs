using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using RESTable.Internal;
using RESTable.Linq;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Results;

namespace RESTable.Client
{
    internal class RemoteRequest : IRequest
    {
        public RESTableContext Context => ProtocolHolder.Context;
        public Headers Headers => ProtocolHolder.Headers;
        public string ProtocolIdentifier => ProtocolHolder.ProtocolIdentifier;
        public IResource Resource => RemoteResource;
        public bool HasConditions => false;
        public bool ExcludeHeaders => false;
        public bool IsValid => true;
        public CachedProtocolProvider CachedProtocolProvider => ProtocolHolder.CachedProtocolProvider;
        public MessageType MessageType => MessageType.HttpInput;
        public Cookies Cookies => ProtocolHolder.Context.Client.Cookies;
        public IUriComponents UriComponents => null;

        private IProtocolHolder ProtocolHolder { get; }
        private Uri URI { get; }
        public Method Method { get; set; }
        private RemoteResource RemoteResource { get; set; }
        public Body Body { get; set; }
        public Type TargetType => null;
        private MetaConditions _metaConditions;
        public MetaConditions MetaConditions => _metaConditions ??= new MetaConditions();
        private Headers _responseHeaders;
        public Headers ResponseHeaders => _responseHeaders ??= new Headers();
        public TimeSpan TimeElapsed { get; private set; }
        public ValueTask<string> GetLogMessage() => new($"{Method} {URI}{Body.GetLengthLogString()}");
        public async ValueTask<string> GetLogContent() => await Body.ToStringAsync();
        public string HeadersStringCache { get; set; }
        public DateTime LogTime { get; }

        public async Task<IResult> Evaluate()
        {
            try
            {
                await Body.Initialize();
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
                            message.Content = new StreamContent(Body);
                            message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(Headers.ContentType.ToString());
                        }
                        break;
                }
                Headers.Metadata = "full";
                Headers.ContentType = null;
                Headers.ForEach(header => message.Headers.Add(header.Key, header.Value));
                using var response = await HttpClientManager.HttpClient.SendAsync(message);
                switch (response?.StatusCode)
                {
                    case null:
                    case HttpStatusCode.GatewayTimeout:
                    case HttpStatusCode.RequestTimeout: return new Timeout(URI.ToString()).AsResultOf(this);
                    case HttpStatusCode.BadGateway: return new BadGateway(URI.ToString()).AsResultOf(this);
                }
                var responseHeaders = new Headers();
                response.Headers.ForEach(header => responseHeaders[header.Key] = header.Value.FirstOrDefault());
                response.Content.Headers.ForEach(header => responseHeaders[header.Key] = header.Value.FirstOrDefault());
                var metadata = responseHeaders.Metadata?.Split(';');
                if (metadata?.Length != 3)
                    return new ExternalServiceNotRESTable(URI).AsResultOf(this);
                var (resultType, resourceName, data) = (metadata[0], metadata[1], metadata[2]);
                if (string.IsNullOrWhiteSpace(resultType))
                    return new ExternalServiceNotRESTable(URI).AsResultOf(this);
                RemoteResource = new RemoteResource(resourceName);

                async Task<IResult> getResult()
                {
                    int nr;
                    ErrorCodes ec;
                    switch (resultType)
                    {
                        case nameof(Entities<object>) when ulong.TryParse(responseHeaders.EntityCount, out var count):
                        {
                            var entitiesResult = new RemoteEntities(this);
                            using var responseStream = response.Content;
                            if (responseStream != null)
                                await response.Content.CopyToAsync(entitiesResult.SerializedResult.Body);
                            return entitiesResult;
                        }
                        case nameof(Head) when ulong.TryParse(data, out var count): return new Head(this, count);
                        case nameof(Report) when ulong.TryParse(data, out var count): return new Report(this, count);
                        case nameof(Binary):
                        {
                            var binaryResult = new Binary(this, responseHeaders.ContentType.GetValueOrDefault());
                            using var responseStream = response.Content;
                            if (responseStream != null)
                                await response.Content.CopyToAsync(binaryResult.Stream);
                            return binaryResult;
                        }
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

                var result = await getResult();
                if (result is Error error)
                    result = error.AsResultOf(this);
                responseHeaders.ForEach(result.Headers.Put);
                TimeElapsed = sw.Elapsed;
                return result;
            }
            catch (Exception e)
            {
                return new ExternalServiceNotRESTable(URI, e).AsResultOf(this);
            }
        }

        public object GetService(Type serviceType)
        {
            return Context.Services.GetService(serviceType);
        }

        public RemoteRequest(RemoteContext context, Method method, string uri, object body, Headers headers)
        {
            ProtocolHolder = new DefaultProtocolHolder(context, headers);
            if (context.HasApiKey)
                Headers.Authorization = $"apikey {context.ApiKey}";
            Method = method;
            Body = new Body(ProtocolHolder, body);
            URI = new Uri(context.ServiceRoot + uri);
            LogTime = DateTime.Now;
        }

        private RemoteRequest(RemoteContext context, IProtocolHolder protocolHolder, Method method, Uri uri, Body bodyCopy)
        {
            ProtocolHolder = protocolHolder;
            if (context.HasApiKey)
                Headers.Authorization = $"apikey {context.ApiKey}";
            Method = method;
            URI = uri;
            Body = bodyCopy;
            LogTime = DateTime.Now;
        }

        public async Task<IRequest> GetCopy(string newProtocol = null) => new RemoteRequest
        (
            context: (RemoteContext) Context,
            protocolHolder: ProtocolHolder,
            method: Method,
            uri: URI,
            bodyCopy: await Body.GetCopy()
        );

        public void Dispose()
        {
            Body.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await Body.DisposeAsync();
        }
    }
}