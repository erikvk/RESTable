﻿using System;
using System.Threading;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Results;

namespace RESTable.Requests
{
    internal class InvalidParametersRequest : IRequest
    {
        public bool IsValid { get; }
        private Exception Error { get; }

        public Task<IResult> GetResult(CancellationToken cancellationToken = new())
        {
            return Task.FromResult<IResult>(Error.AsResultOf(this));
        }

        public ITarget Target { get; }
        public bool HasConditions => false;

        #region Logable

        private ILogable LogItem => Parameters;
        private IHeaderHolder HeaderHolder => Parameters;
        MessageType ILogable.MessageType => LogItem.MessageType;
        ValueTask<string> ILogable.GetLogMessage() => LogItem.GetLogMessage();
        ValueTask<string?> ILogable.GetLogContent() => LogItem.GetLogContent();

        /// <inheritdoc />
        public DateTime LogTime { get; }

        string? IHeaderHolder.HeadersStringCache
        {
            get => HeaderHolder.HeadersStringCache;
            set => HeaderHolder.HeadersStringCache = value;
        }

        bool IHeaderHolder.ExcludeHeaders => HeaderHolder.ExcludeHeaders;

        #endregion

        #region Parameter bindings

        public RequestParameters Parameters { get; }
        public string ProtocolIdentifier => Parameters.ProtocolIdentifier;
        public RESTableContext Context => Parameters.Context;
        public CachedProtocolProvider CachedProtocolProvider => Parameters.CachedProtocolProvider;
        public IUriComponents UriComponents => Parameters.UriComponents;
        public Headers Headers => Parameters.Headers;
        public IResource Resource { get; }
        public TimeSpan TimeElapsed => default;
        public object? GetService(Type serviceType) => Context.GetService(serviceType);
        public IContentTypeProvider InputContentTypeProvider => Parameters.InputContentTypeProvider;
        public IContentTypeProvider OutputContentTypeProvider => Parameters.OutputContentTypeProvider;

        #endregion

        public Method Method { get; set; }
        public MetaConditions MetaConditions { get; }
        public Headers ResponseHeaders { get; }
        public Cookies Cookies => Context.Client.Cookies;
        public Body Body { get; set; }

        internal InvalidParametersRequest(RequestParameters parameters)
        {
            IsValid = false;
            Parameters = parameters;
            LogTime = DateTime.Now;
            MetaConditions = null!;
            ResponseHeaders = null!;
            Error = parameters.Error!;
            Resource = parameters.iresource!;
            Method = parameters.Method;
            Body = parameters.Body;

            // These are always null for invalid requests
            Target = null!;
            MetaConditions = null!;
            ResponseHeaders = null!;
        }

        public Task<IRequest> GetCopy(string? newProtocol = null)
        {
            // We do not care about changing the protocol of an invalid parameters request.
            return Task.FromResult<IRequest>(new InvalidParametersRequest(Parameters));
        }

        public void Dispose() => Body.Dispose();
        public async ValueTask DisposeAsync() => await Body.DisposeAsync().ConfigureAwait(false);
    }
}