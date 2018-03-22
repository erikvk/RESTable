using System;
using System.Collections.Generic;
using RESTar.Internal;
using RESTar.Logging;
using RESTar.Operations;
using RESTar.Results.Error;

namespace RESTar.Requests
{
    internal class InternalRequest<T> : IRequest<T>, IRequestInternal<T> where T : class
    {
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

        #region Parameter bindings

        public RequestParameters RequestParameters { get; }
        public Methods Method => RequestParameters.Method;
        public string TraceId => RequestParameters.TraceId;
        public Client Client => RequestParameters.Client;
        public IUriParameters UriParameters => RequestParameters.Uri;
        public Headers Headers => RequestParameters.Headers;
        private bool IsWebSocketUpgrade => RequestParameters.IsWebSocketUpgrade;

        #endregion

        IEntityResource IRequest.Resource => Resource;
        public MetaConditions MetaConditions { get; }
        public Body Body { get; set; }
        public Headers ResponseHeaders { get; }
        public ICollection<string> Cookies { get; }

        public IResult GetResult()
        {
            throw new NotImplementedException();
        }

        public bool IsValid { get; }
        public IEntityResource<T> Resource { get; }
        public Condition<T>[] Conditions { get; set; }
        public ITarget<T> Target { get; }

        public IEnumerable<T> GetEntities() => EntitiesGenerator?.Invoke() ?? new T[0];

        public string Destination { get; }

        public Func<IEnumerable<T>> EntitiesGenerator { get; set; }
    }
}