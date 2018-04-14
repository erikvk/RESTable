using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Logging;
using RESTar.Resources;
using RESTar.Results;
using RESTar.Serialization;

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

        public void SetBody(object content)
        {
            var bytes = content != null ? Serializers.Json.SerializeToBytes(content) : new byte[0];
            var contentType = Serializers.Json.ContentType;
            Body = new Body(bytes, contentType, CachedProtocolProvider);
        }

        public void SetBody(byte[] bytes, ContentType? contentType = null)
        {
            var _contentType = contentType ?? Headers.ContentType ?? CachedProtocolProvider.DefaultInputProvider.ContentType;
            Body = new Body(bytes, _contentType, CachedProtocolProvider);
        }

        public IResource Resource => throw new InvalidOperationException(ErrorMessage(nameof(Resource)));
        public Type TargetType => throw new InvalidOperationException(ErrorMessage(nameof(TargetType)));
        public bool HasConditions => throw new InvalidOperationException(ErrorMessage(nameof(HasConditions)));
        public MetaConditions MetaConditions => throw new InvalidOperationException(ErrorMessage(nameof(MetaConditions)));
        public Headers ResponseHeaders => throw new InvalidOperationException(ErrorMessage(nameof(ResponseHeaders)));
        public ICollection<string> Cookies => throw new InvalidOperationException(ErrorMessage(nameof(Cookies)));
        public IUriComponents UriComponents => throw new InvalidOperationException(ErrorMessage(nameof(UriComponents)));

        public IResult Result => GetResult().Result;

        private async Task<IResult> GetResult()
        {
            try
            {
                var method = new HttpMethod(Method.ToString());
                var message = new HttpRequestMessage(method, URI) {Content = new ByteArrayContent(Body.Bytes)};
                Headers.Metadata = "full";
                Headers.Accept = ContentType.JSON;
                Headers.ForEach(header => message.Headers.Add(header.Key, header.Value));
                var response = await HttpClient.SendAsync(message);
                var responseHeaders = new Headers();
                response.Headers.ForEach(header => responseHeaders[header.Key] = header.Value.FirstOrDefault());
                var metadata = responseHeaders.Metadata?.Split(';');
                if (metadata?.Length != 3)
                    return new ExternalServiceNotRESTar(URI);
                var (resultType, resourceName, data) = (metadata[0], metadata[1], metadata[2]);
                if (resultType == null || resourceName == null)
                    return new ExternalServiceNotRESTar(URI);
                RemoteResource = new RemoteResource(resourceName);
                var stream = new RESTarOutputStreamController();
                if (response.Content != null)
                {
                    await response.Content.CopyToAsync(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                }
                switch (resultType)
                {
                    case "RESTar.Results.Entities": return new RemoteEntities(this, stream);
                }
                return null;
            }
            catch (Exception e)
            {
                return new ExternalServiceNotRESTar(URI, e);
            }
        }

        public bool IsValid { get; }
        public TimeSpan TimeElapsed { get; private set; }
        public bool IsWebSocketUpgrade { get; }
        public CachedProtocolProvider CachedProtocolProvider { get; }

        public LogEventType LogEventType { get; }
        public string LogMessage => $"{Method} {URI}{(Body.HasContent ? $" ({Body.Bytes.Length} bytes)" : "")}";
        public string LogContent => Body.ToString();
        public string HeadersStringCache { get; set; }
        public bool ExcludeHeaders { get; }
        public DateTime LogTime { get; }

        public RemoteRequest(RemoteContext context, Method method, string uri, byte[] body, Headers headers)
        {
            TraceId = context.InitialTraceId;
            Context = context;
            Headers = new Headers();
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
            Headers = headers ?? new Headers();
        }
    }
}