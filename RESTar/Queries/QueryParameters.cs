using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Web;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Logging;
using RESTar.Resources;
using RESTar.Results.Error;
using RESTar.Serialization;
using RESTar.WebSockets;
using IResource = RESTar.Resources.IResource;

namespace RESTar.Queries
{
    /// <inheritdoc cref="ILogable" />
    /// <inheritdoc cref="ITraceable" />
    /// <summary>
    /// A class that defines the parameters of a call to a RESTar API. APICall is a unified 
    /// way to talk about the input to request evaluation, regardless of protocol and web technologies.
    /// </summary>
    public class QueryParameters : ILogable, ITraceable
    {
        /// <inheritdoc />
        public string TraceId { get; }

        /// <inheritdoc />
        public Context Context { get; }

        /// <summary>
        /// The method to perform
        /// </summary>
        public Method Method { get; }

        /// <summary>
        /// The URI contained in the arguments
        /// </summary>
        public URI Uri { get; }

        /// <summary>
        /// Did the request contain a body?
        /// </summary>
        public bool HasBody { get; }

        /// <summary>
        /// The byte array of the request body
        /// </summary>
        public byte[] BodyBytes { get; }

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

        internal CachedProtocolProvider CachedProtocolProvider { get; }
        private string UnparsedUri { get; }
        private IResource iresource;
        internal IResource IResource => iresource ?? (iresource = Resource.Find(Uri.ResourceSpecifier));
        internal Exception Error { get; set; }
        private static bool PercentCharsEscaped(IDictionary<string, string> headers) => headers?.ContainsKey("X-ARR-LOG-ID") == true;
        private static string UnpackUriKey(string uriKey) => uriKey != null ? HttpUtility.UrlDecode(uriKey).Substring(1, uriKey.Length - 2) : null;
        bool ILogable.ExcludeHeaders => IResource is IEntityResource e && e.RequiresAuthentication;
        LogEventType ILogable.LogEventType { get; } = LogEventType.HttpInput;
        string ILogable.LogMessage => $"{Method} {UnparsedUri}{(HasBody ? $" ({BodyBytes.Length} bytes)" : "")}";
        DateTime ILogable.LogTime { get; } = DateTime.Now;
        string ILogable.HeadersStringCache { get; set; }
        private string _contentString;

        string ILogable.LogContent
        {
            get
            {
                if (!HasBody) return null;
                return _contentString ?? (_contentString = Encoding.UTF8.GetString(BodyBytes));
            }
        }

        #endregion

        internal QueryParameters(Context context, Method method, IResource resource, string protocolIdentifier = null, string viewName = null)
        {
            TraceId = context.InitialTraceId;
            Context = context;
            Method = method;
            Headers = new Headers();
            iresource = resource;
            IsWebSocketUpgrade = Context.WebSocket?.Status == WebSocketStatus.Waiting;
            Uri = new URI
            {
                ResourceSpecifier = resource.Name,
                ViewName = viewName
            };
            CachedProtocolProvider = ProtocolController.ResolveProtocolProvider(protocolIdentifier);
        }

        internal QueryParameters(Context context, Method method, ref string uri, byte[] body, Headers headers)
        {
            TraceId = context.InitialTraceId;
            Context = context;
            Method = method;
            Headers = headers ?? new Headers();
            IsWebSocketUpgrade = Context.WebSocket?.Status == WebSocketStatus.Waiting;
            Uri = URI.ParseInternal(ref uri, PercentCharsEscaped(headers), context, out var key, out var cachedProtocolProvider);
            var hasMacro = Uri?.Macro != null;
            if (hasMacro)
            {
                if (Uri.Macro.OverWriteHeaders)
                    Uri.Macro.HeadersDictionary?.ForEach(pair => Headers[pair.Key] = pair.Value);
                else
                {
                    Uri.Macro.HeadersDictionary?.ForEach(pair =>
                    {
                        var currentValue = Headers.SafeGet(pair.Key);
                        if (string.IsNullOrWhiteSpace(currentValue) || currentValue == "*/*")
                            Headers[pair.Key] = pair.Value;
                    });
                }
            }

            CachedProtocolProvider = cachedProtocolProvider;
            if (key != null)
                Headers["Authorization"] = $"apikey {UnpackUriKey(key)}";
            UnparsedUri = uri;
            try
            {
                var _ = IResource;
            }
            catch (Exception e)
            {
                Error = RESTarError.GetError(e);
            }

            if (hasMacro)
            {
                if (Uri.Macro.OverWriteBody)
                {
                    if (Uri.Macro.HasBody)
                    {
                        BodyBytes = Uri.Macro.GetBody();
                        Headers.ContentType = Serializers.Json.ContentType;
                    }
                }
                else
                {
                    if (!(body?.Length > 0) && Uri.Macro.HasBody)
                    {
                        BodyBytes = Uri.Macro.GetBody();
                        Headers.ContentType = Serializers.Json.ContentType;
                    }
                    else BodyBytes = body;
                }
            }
            else BodyBytes = body;
            HasBody = BodyBytes?.Length > 0;
            if (Error == null && Uri?.HasError == true)
                Error = Uri.Error;
        }
    }
}