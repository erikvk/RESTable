using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.WebSockets;
using RESTable.Linq;

namespace RESTable.Requests
{
    /// <inheritdoc cref="ILogable" />
    /// <inheritdoc cref="ITraceable" />
    /// <summary>
    /// A class that defines the parameters of a call to a RESTable API. A unified 
    /// way to talk about the input to request evaluation, regardless of protocol
    /// and web technologies.
    /// </summary>
    internal class RequestParameters : ILogable, IHeaderHolder, IProtocolHolder, ITraceable
    {
        /// <inheritdoc />
        public string TraceId { get; }

        /// <inheritdoc /> 
        public RESTableContext Context { get; }

        public string ProtocolIdentifier { get; }

        /// <summary>
        /// The method to perform
        /// </summary>
        public Method Method { get; set; }

        private URI Uri { get; }

        /// <summary>
        /// The uri components contained in the arguments
        /// </summary>
        public IUriComponents UriComponents => Uri;

        /// <summary>
        /// Did the request contain a body?
        /// </summary>
        public bool HasBody => Body != null && Body.HasContent;

        private Body _body;

        /// <summary>
        /// The object that should form the request body
        /// </summary>
        public Body Body
        {
            get => _body;
            set
            {
                _body?.DisposeAsync().AsTask().Wait();
                _body = value;
            }
        }

        /// <inheritdoc />
        public Headers Headers { get; }

        /// <summary>
        /// Are these request parameters valid?
        /// </summary>
        public bool IsValid => Error == null;

        /// <summary>
        /// Has the client requested a WebSocket upgrade for this request?
        /// </summary>
        public bool IsWebSocketUpgrade { get; }

        internal Stopwatch Stopwatch { get; } = Stopwatch.StartNew();

        #region Private and internal

        private string UnparsedUri { get; }
        internal IResource iresource;
        internal IResource IResource => iresource ??= Resource.Find(Uri.ResourceSpecifier);
        internal Exception Error { get; }
        private static bool PercentCharsEscaped(IDictionary<string, string> headers) => headers?.ContainsKey("X-ARR-LOG-ID") == true;
        bool IHeaderHolder.ExcludeHeaders => IResource is IEntityResource {RequiresAuthentication: true} e;
        public MessageType MessageType { get; } = MessageType.HttpInput;

        public CachedProtocolProvider CachedProtocolProvider { get; set; }

        ValueTask<string> ILogable.GetLogMessage()
        {
            var message = $"{Method} {UnparsedUri}";
            if (HasBody)
                return new ValueTask<string>(message + Body.GetLengthLogString());
            return new ValueTask<string>(message);
        }

        DateTime ILogable.LogTime { get; } = DateTime.Now;
        public string HeadersStringCache { get; set; }

        async ValueTask<string> ILogable.GetLogContent()
        {
            if (!HasBody) return null;
            return await Body.ToStringAsync();
        }

        internal async Task<RequestParameters> GetCopy() => new(
            iresource: iresource,
            context: Context,
            method: Method,
            uri: Uri,
            bodyCopy: await Body.GetCopy(),
            headers: Headers,
            isWebSocketUpgrade: IsWebSocketUpgrade,
            unparsedUri: UnparsedUri,
            error: Error,
            messageType: MessageType,
            cachedProtocolProvider: CachedProtocolProvider,
            protocolIdentifier: ProtocolIdentifier,
            headersStringCache: HeadersStringCache
        );

        #endregion

        private RequestParameters(IResource iresource, RESTableContext context, Method method, URI uri, Body bodyCopy, Headers headers, bool isWebSocketUpgrade, string unparsedUri, Exception error, MessageType messageType,
            CachedProtocolProvider cachedProtocolProvider, string protocolIdentifier, string headersStringCache)
        {
            this.iresource = iresource;
            Context = context;
            Method = method;
            Uri = uri;
            Body = bodyCopy;
            Headers = headers;
            IsWebSocketUpgrade = isWebSocketUpgrade;
            UnparsedUri = unparsedUri;
            Error = error;
            MessageType = messageType;
            CachedProtocolProvider = cachedProtocolProvider;
            ProtocolIdentifier = protocolIdentifier;
            HeadersStringCache = headersStringCache;
        }

        internal void SetBody(object bodyObject) => Body = new Body(this, bodyObject);

        /// <summary>
        /// Used when creating generic requests through the .NET API
        /// </summary>
        internal RequestParameters(RESTableContext context, Method method, IResource resource, string protocolIdentifier = null, string viewName = null)
        {
            TraceId = context.InitialTraceId;
            Context = context;
            Method = method;
            Headers = new Headers();
            iresource = resource;
            IsWebSocketUpgrade = Context.WebSocket?.Status == WebSocketStatus.Waiting;
            Uri = new URI(resourceSpecifier: resource.Name, viewName: viewName);
            ProtocolIdentifier = protocolIdentifier?.ToLower() ?? ProtocolController.DefaultProtocolProvider.ProtocolProvider.ProtocolIdentifier;
            CachedProtocolProvider = ProtocolController.ResolveProtocolProvider(protocolIdentifier);
        }

        /// <summary>
        /// Used when creating parsed requests
        /// </summary>
        internal RequestParameters(RESTableContext context, Method method, string uri, Headers headers)
        {
            TraceId = context.InitialTraceId;
            Context = context;
            Method = method;
            Headers = headers ?? new Headers();
            IsWebSocketUpgrade = Context.WebSocket?.Status == WebSocketStatus.Waiting;
            Uri = URI.ParseInternal(uri, PercentCharsEscaped(headers), context, out var cachedProtocolProvider);
            ProtocolIdentifier = cachedProtocolProvider.ProtocolProvider.ProtocolIdentifier;
            var hasMacro = Uri?.Macro != null;
            if (hasMacro)
            {
                if (Uri.Macro.OverwriteHeaders)
                    Uri.Macro.Headers?.ForEach(pair => Headers[pair.Key] = pair.Value);
                else
                {
                    Uri.Macro.Headers?.ForEach(pair =>
                    {
                        var currentValue = Headers.SafeGet(pair.Key);
                        if (string.IsNullOrWhiteSpace(currentValue) || currentValue == "*/*")
                            Headers[pair.Key] = pair.Value;
                    });
                }
            }
            CachedProtocolProvider = cachedProtocolProvider;
            UnparsedUri = uri;
            if (Uri?.HasError == true)
            {
                Error = Uri.Error;
                return;
            }
            try
            {
                var _ = IResource;
            }
            catch (Exception e)
            {
                Error = e.AsError();
            }

            if (hasMacro)
            {
                if (Uri.Macro.OverwriteBody)
                {
                    if (Uri.Macro.HasBody)
                    {
                        Body = Uri.Macro.Body;
                        Headers.ContentType = Providers.Json.ContentType;
                    }
                }
                else
                {
                    if (!HasBody && Uri.Macro.HasBody)
                    {
                        Body = Uri.Macro.Body;
                        Headers.ContentType = Providers.Json.ContentType;
                    }
                }
            }
        }

        /// <summary>
        /// Used when performing CheckOrigin
        /// </summary>
        internal RequestParameters(RESTableContext context, string uri, Headers headers)
        {
            TraceId = context.InitialTraceId;
            Context = context;
            Headers = headers ?? new Headers();
            Uri = URI.ParseInternal(uri, PercentCharsEscaped(headers), context, out var cachedProtocolProvider);
            ProtocolIdentifier = cachedProtocolProvider.ProtocolProvider.ProtocolIdentifier;
            var hasMacro = Uri?.Macro != null;
            if (hasMacro)
            {
                if (Uri.Macro.OverwriteHeaders)
                    Uri.Macro.Headers?.ForEach(pair => Headers[pair.Key] = pair.Value);
                else
                {
                    Uri.Macro.Headers?.ForEach(pair =>
                    {
                        var currentValue = Headers.SafeGet(pair.Key);
                        if (string.IsNullOrWhiteSpace(currentValue) || currentValue == "*/*")
                            Headers[pair.Key] = pair.Value;
                    });
                }
            }
            CachedProtocolProvider = cachedProtocolProvider;
            UnparsedUri = uri;
            try
            {
                var _ = IResource;
            }
            catch (Exception e)
            {
                Error = e.AsError();
            }
            if (Error == null && Uri?.HasError == true)
                Error = Uri.Error;
        }
    }
}