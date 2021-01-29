﻿using System;
using System.Linq;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Results;

namespace RESTable.Requests
{
    internal class InvalidParametersRequest : IRequest, IRequestInternal
    {
        public bool IsValid { get; }
        private Exception Error { get; }
        public IResult Evaluate() => Error.AsResultOf(this);
        public Type TargetType => null;
        public bool HasConditions => false;

        #region Logable

        private ILogable LogItem => Parameters;
        MessageType ILogable.MessageType => LogItem.MessageType;
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
        public RESTableContext Context => Parameters.Context;
        public CachedProtocolProvider CachedProtocolProvider => Parameters.CachedProtocolProvider;
        public IUriComponents UriComponents => Parameters.UriComponents;
        public Headers Headers => Parameters.Headers;
        public IResource Resource { get; }
        public bool IsWebSocketUpgrade => Parameters.IsWebSocketUpgrade;
        public TimeSpan TimeElapsed => Parameters.Stopwatch.Elapsed;
        public void EnsureServiceAttached<T>(T service) where T : class { }
        public void EnsureServiceAttached<TService, TImplementation>(TImplementation service) where TService : class where TImplementation : class, TService { }
        public object GetService(Type serviceType) => null;

        #endregion

        public Method Method { get; set; }
        public MetaConditions MetaConditions { get; }
        private readonly Body body;
        public Body GetBody() => body;
        public Headers ResponseHeaders { get; }
        public Cookies Cookies => Context.Client.Cookies;

        public void SetBody(object content, ContentType? contentType = null) =>
            throw new InvalidOperationException("Cannot set body of an invalid request");

        internal InvalidParametersRequest(RequestParameters parameters)
        {
            IsValid = false;
            Parameters = parameters;
            Error = parameters.Error;
            Resource = parameters.iresource;
            MetaConditions = null;
            Method = parameters.Method;
            var contentType = Headers.ContentType
                              ?? CachedProtocolProvider?.DefaultInputProvider.ContentType
                              ?? ContentType.JSON;
            if (parameters.BodyBytes?.Any() == true)
                body = new Body
                (
                    stream: new RESTableStream
                    (
                        contentType: contentType,
                        buffer: parameters.BodyBytes
                    ),
                    protocolProvider: parameters.CachedProtocolProvider
                );
            ResponseHeaders = null;
        }

        public IRequest GetCopy(string newProtocol = null)
        {
            // We do not care about changing the protocol of an invalid parameters request.
            return new InvalidParametersRequest(Parameters);
        }

        public void Dispose()
        {
            GetBody().Dispose();
        }
    }
}