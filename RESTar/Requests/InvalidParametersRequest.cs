using System;
using System.Collections.Generic;
using System.IO;
using RESTar.Internal;
using RESTar.Logging;
using RESTar.Resources;

namespace RESTar.Requests
{
    internal class InvalidParametersRequest : IRequest, IRequestInternal
    {
        public bool IsValid { get; }
        private Exception Error { get; }
        public IResult Result => Error.AsResultOf(this);
        public Type TargetType => null;
        public bool HasConditions => false;

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

        public RequestParameters Parameters { get; }
        public string TraceId => Parameters.TraceId;
        public Context Context => Parameters.Context;
        public CachedProtocolProvider CachedProtocolProvider => Parameters.CachedProtocolProvider;
        public IUriComponents UriComponents => Parameters.Uri;
        public Headers Headers => Parameters.Headers;
        public IResource Resource { get; }
        public bool IsWebSocketUpgrade => Parameters.IsWebSocketUpgrade;
        public TimeSpan TimeElapsed => Parameters.Stopwatch.Elapsed;

        #endregion

        public Method Method { get; set; }
        public MetaConditions MetaConditions { get; }
        public Body Body { get; }
        public Headers ResponseHeaders { get; }
        public ICollection<string> Cookies { get; }
        public void SetBody(object content) => throw new InvalidOperationException("Cannot set body of an invalid request");

        public void SetBody(byte[] bytes, ContentType? contentType) => throw new InvalidOperationException("Cannot set body of an invalid request");

        internal InvalidParametersRequest(RequestParameters parameters)
        {
            IsValid = false;
            Parameters = parameters;
            Error = parameters.Error;
            Resource = parameters.iresource;
            MetaConditions = null;
            Method = parameters.Method;
            Body = new Body
            (
                stream: new MemoryStream(parameters.BodyBytes),
                contentType: Headers.ContentType
                             ?? CachedProtocolProvider?.DefaultInputProvider.ContentType
                             ?? Serialization.Serializers.Json.ContentType,
                protocolProvider: parameters.CachedProtocolProvider
            );
            ResponseHeaders = null;
            Cookies = null;
        }
    }
}