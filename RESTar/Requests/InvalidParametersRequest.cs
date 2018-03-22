using System;
using System.Collections.Generic;
using RESTar.Internal;
using RESTar.Logging;
using RESTar.Operations;
using RESTar.Results.Error;

namespace RESTar.Requests
{
    internal class InvalidParametersRequest : IRequest, IRequestInternal
    {
        public bool IsValid { get; }
        private Exception Error { get; }
        public IResult GetResult() => RESTarError.GetResult(Error, this);

        #region Logable

        private ILogable LogItem => RequestParameters;
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

        public RequestParameters RequestParameters { get; }
        public Methods Method => RequestParameters.Method;
        public string TraceId => RequestParameters.TraceId;
        public Client Client => RequestParameters.Client;
        public CachedProtocolProvider ProtocolProvider => RequestParameters.CachedProtocolProvider;
        public IUriComponents UriComponents => RequestParameters.Uri;
        public Headers Headers => RequestParameters.Headers;
        public IEntityResource Resource => RequestParameters.IResource as IEntityResource;
        public bool IsWebSocketUpgrade => RequestParameters.IsWebSocketUpgrade;

        #endregion

        public MetaConditions MetaConditions { get; }
        public Body Body { get; set; }
        public Headers ResponseHeaders { get; }
        public ICollection<string> Cookies { get; }

        internal InvalidParametersRequest(RequestParameters parameters)
        {
            IsValid = false;
            RequestParameters = parameters;
            Error = parameters.Error;
            MetaConditions = null;
            Body = new Body
            (
                bytes: parameters.BodyBytes,
                contentType: Headers.ContentType
                             ?? ProtocolProvider?.DefaultInputProvider.ContentType
                             ?? Serialization.Serializers.Json.ContentType
            );
            ResponseHeaders = null;
            Cookies = null;
        }
    }
}