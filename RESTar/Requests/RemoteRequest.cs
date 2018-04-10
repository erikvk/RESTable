using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using RESTar.Internal;
using RESTar.Logging;
using RESTar.Resources;

namespace RESTar.Requests
{
    internal class RemoteRequest : IRequest, IRequestInternal
    {
        private static readonly HttpClient HttpClient;
        static RemoteRequest() => HttpClient = new HttpClient();

        public string TraceId { get; }
        public Context Context { get; }

        public Headers Headers { get; }
        public Method Method { get; set; }
        public IEntityResource Resource { get; }
        public Type TargetType { get; }
        public bool HasConditions { get; }
        public MetaConditions MetaConditions { get; }
        public Body Body { get; }

        public void SetBody(object content)
        {
            throw new NotImplementedException();
        }

        public void SetBody(byte[] bytes, ContentType? contentType = null)
        {
            throw new NotImplementedException();
        }

        public Headers ResponseHeaders { get; }
        public ICollection<string> Cookies { get; }
        public IUriComponents UriComponents { get; }
        public IResult Result { get; }
        public bool IsValid { get; }
        public TimeSpan TimeElapsed { get; }
        public bool IsWebSocketUpgrade { get; }
        public CachedProtocolProvider CachedProtocolProvider { get; }

        public LogEventType LogEventType { get; }
        public string LogMessage { get; }
        public string LogContent { get; }
        public string HeadersStringCache { get; set; }
        public bool ExcludeHeaders { get; }
        public DateTime LogTime { get; }

        public RemoteRequest(Context context, Method method, string uri, byte[] body, Headers headers)
        {
            TraceId = context.InitialTraceId;
            Context = context;
            Headers = new Headers();
            Method = method;

        }
    }
}