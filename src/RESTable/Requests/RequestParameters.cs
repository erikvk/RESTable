using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.WebSockets;

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
        public bool HasBody => Body is {CanRead: true};

        private Body _body;

        /// <summary>
        /// The object that should form the request body
        /// </summary>
        public Body Body
        {
            get => _body;
            set
            {
                if (Equals(_body, value)) return;
                _body.DisposeAsync().AsTask().Wait();
                _body = value;
            }
        }

        /// <inheritdoc />
        public Headers Headers { get; }

        /// <summary>
        /// Are these request parameters valid?
        /// </summary>
        public bool IsValid => Error is null;

        /// <summary>
        /// Has the client requested a WebSocket upgrade for this request?
        /// </summary>
        public bool IsWebSocketUpgrade => Context.WebSocket?.Status == WebSocketStatus.Waiting;

        #region Private and internal

        private string UnparsedUri { get; }
        internal IResource iresource;
        internal IResource IResource => iresource ??= Context.GetRequiredService<ResourceCollection>().FindResource(Uri.ResourceSpecifier);
        internal Exception? Error { get; }
        private static bool PercentCharsEscaped(IDictionary<string, string> headers) => headers?.ContainsKey("X-ARR-LOG-ID") == true;
        bool IHeaderHolder.ExcludeHeaders => IResource is IEntityResource {RequiresAuthentication: true};
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
        public string? HeadersStringCache { get; set; }

        async ValueTask<string> ILogable.GetLogContent()
        {
            if (!HasBody) return null;
            return await Body.ToStringAsync().ConfigureAwait(false);
        }

        internal async Task<RequestParameters> GetCopy() => new
        (
            iresource: iresource,
            context: Context,
            method: Method,
            uri: Uri,
            bodyCopy: await Body.GetCopy().ConfigureAwait(false),
            headers: Headers,
            unparsedUri: UnparsedUri,
            error: Error,
            messageType: MessageType,
            cachedProtocolProvider: CachedProtocolProvider,
            protocolIdentifier: ProtocolIdentifier,
            headersStringCache: HeadersStringCache
        );

        #endregion

        private RequestParameters(IResource iresource, RESTableContext context, Method method, URI uri, Body bodyCopy, Headers headers, string unparsedUri,
            Exception error, MessageType messageType, CachedProtocolProvider cachedProtocolProvider, string protocolIdentifier, string headersStringCache)
        {
            this.iresource = iresource;
            Context = context;
            Method = method;
            Uri = uri;
            _body = bodyCopy;
            Headers = headers;
            UnparsedUri = unparsedUri;
            Error = error;
            MessageType = messageType;
            CachedProtocolProvider = cachedProtocolProvider;
            ProtocolIdentifier = protocolIdentifier;
            HeadersStringCache = headersStringCache;
        }

        /// <summary>
        /// Used when creating generic requests through the .NET API
        /// </summary>
        internal RequestParameters(RESTableContext context, Method method, IResource resource, string? protocolIdentifier = null, string? viewName = null)
        {
            Context = context;
            Method = method;
            Headers = new Headers();
            iresource = resource;
            Uri = new URI(resourceSpecifier: resource.Name, viewName: viewName);
            var protocolController = context.GetRequiredService<ProtocolProviderManager>();
            ProtocolIdentifier = protocolIdentifier?.ToLower() ?? protocolController.DefaultProtocolProvider.ProtocolProvider.ProtocolIdentifier;
            CachedProtocolProvider = protocolController.ResolveCachedProtocolProvider(protocolIdentifier);
            _body = new Body(this);
        }

        /// <summary>
        /// Used when creating parsed requests
        /// </summary>
        internal RequestParameters(RESTableContext context, Method method, string uri, Headers? headers, object? body)
        {
            Context = context;
            Method = method;
            Headers = headers ?? new Headers();
            Uri = URI.ParseInternal(uri, PercentCharsEscaped(headers), context, out var cachedProtocolProvider);
            ProtocolIdentifier = cachedProtocolProvider.ProtocolProvider.ProtocolIdentifier;
            _body = new Body(this, body);
            var hasMacro = Uri?.Macro is not null;
            if (hasMacro && Uri.Macro.Headers is not null)
            {
                if (Uri.Macro.OverwriteHeaders)
                {
                    foreach (var (key, value) in Uri.Macro.Headers)
                        Headers[key] = value;
                }
                else
                {
                    foreach (var (key, value) in Uri.Macro.Headers)
                    {
                        var currentValue = Headers.SafeGet(key);
                        if (string.IsNullOrWhiteSpace(currentValue) || currentValue == "*/*")
                            Headers[key] = value;
                    }
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
        }

        /// <summary>
        /// Used when performing CheckOrigin
        /// </summary>
        internal RequestParameters(RESTableContext context, string uri, Headers? headers)
        {
            Context = context;
            Headers = headers ?? new Headers();
            Uri = URI.ParseInternal(uri, PercentCharsEscaped(headers), context, out var cachedProtocolProvider);
            ProtocolIdentifier = cachedProtocolProvider.ProtocolProvider.ProtocolIdentifier;
            var hasMacro = Uri?.Macro is not null;
            if (hasMacro && Uri.Macro.Headers is not null)
            {
                if (Uri.Macro.OverwriteHeaders)
                {
                    foreach (var (key, value) in Uri.Macro.Headers)
                        Headers[key] = value;
                }
                else
                {
                    foreach (var (key, value) in Uri.Macro.Headers)
                    {
                        var currentValue = Headers.SafeGet(key);
                        if (string.IsNullOrWhiteSpace(currentValue) || currentValue == "*/*")
                            Headers[key] = value;
                    }
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
            if (Error is null && Uri?.HasError == true)
                Error = Uri.Error;
        }
    }
}