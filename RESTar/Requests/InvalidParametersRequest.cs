using System;
using System.Collections.Generic;
using RESTar.Internal;
using RESTar.Logging;
using RESTar.Operations;
using RESTar.Results.Error;

namespace RESTar.Queries
{
    internal class InvalidParametersQuery : IQuery, IQueryInternal
    {
        public bool IsValid { get; }
        private Exception Error { get; }
        public IResult Result => RESTarError.GetResult(Error, this);

        #region Logable

        private ILogable LogItem => Parameters;
        LogEventType ILogable.LogEventType => LogItem.LogEventType;
        string ILogable.LogMessage => LogItem.LogMessage;
        string ILogable.LogContent => LogItem.LogContent;

        /// <inheritdoc />
        public DateTime LogTime { get; } = DateTime.Now;

        string ILogable.HeadersStringCache
        {
            get => LogItem.HeadersStringCache;
            set => LogItem.HeadersStringCache = value;
        }

        bool ILogable.ExcludeHeaders => LogItem.ExcludeHeaders;

        #endregion

        #region Parameter bindings

        public QueryParameters Parameters { get; }
        public string TraceId => Parameters.TraceId;
        public Context Context => Parameters.Context;
        public CachedProtocolProvider CachedProtocolProvider => Parameters.CachedProtocolProvider;
        public IUriComponents UriComponents => Parameters.Uri;
        public Headers Headers => Parameters.Headers;
        public IEntityResource Resource => Parameters.IResource as IEntityResource;
        public bool IsWebSocketUpgrade => Parameters.IsWebSocketUpgrade;
        public TimeSpan TimeElapsed => Parameters.Stopwatch.Elapsed;

        #endregion

        public Method Method { get; set; }
        public MetaConditions MetaConditions { get; }
        public Body Body { get; set; }
        public Headers ResponseHeaders { get; }
        public ICollection<string> Cookies { get; }
        
        internal InvalidParametersQuery(QueryParameters parameters)
        {
            IsValid = false;
            Parameters = parameters;
            Error = parameters.Error;
            MetaConditions = null;
            Method = parameters.Method;
            Body = new Body
            (
                bytes: parameters.BodyBytes,
                contentType: Headers.ContentType
                             ?? CachedProtocolProvider?.DefaultInputProvider.ContentType
                             ?? Serialization.Serializers.Json.ContentType
            );
            ResponseHeaders = null;
            Cookies = null;
        }
    }
}