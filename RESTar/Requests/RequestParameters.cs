using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Logging;
using RESTar.Serialization;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
    /// <summary>
    /// A common interface for URI conditions in RESTar
    /// </summary>
    public interface IUriCondition
    {
        /// <summary>
        /// The key of the condition
        /// </summary>
        string Key { get; }

        /// <summary>
        /// The operator of the condition
        /// </summary>
        Operators Operator { get; }

        /// <summary>
        /// A string describing the value encoded in the condition
        /// </summary>
        string ValueLiteral { get; }
    }

    /// <summary>
    /// Contains parameters for a RESTar URI
    /// </summary>
    public interface IUriComponents
    {
        /// <summary>
        /// Specifies the resource for the request
        /// </summary>
        string ResourceSpecifier { get; }

        /// <summary>
        /// Specifies the view for the request
        /// </summary>
        string ViewName { get; }

        /// <summary>
        /// Specifies the conditions for the request
        /// </summary>
        IEnumerable<IUriCondition> Conditions { get; }

        /// <summary>
        /// Specifies the meta-conditions for the request
        /// </summary>
        IEnumerable<IUriCondition> MetaConditions { get; }

        /// <summary>
        /// Generates a URI string from these components, according to some protocol.
        /// If null, the default protocol is used.
        /// </summary>
        string ToUriString(string protocolIdentifier = null);
    }

    /// <inheritdoc cref="ILogable" />
    /// <inheritdoc cref="ITraceable" />
    /// <summary>
    /// A class that defines the parameters of a call to a RESTar API. APICall is a unified 
    /// way to talk about the input to request evaluation, regardless of protocol and web technologies.
    /// </summary>
    public class RequestParameters : ILogable, ITraceable
    {
        /// <inheritdoc />
        public string TraceId { get; }

        /// <inheritdoc />
        public Client Client { get; }

        /// <summary>
        /// The method to perform
        /// </summary>
        public Methods Method { get; }

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

        internal RequestParameters(ITraceable trace, Methods method, IResource resource, string protocolIdentifier = null)
        {
            TraceId = trace.TraceId;
            Client = trace.Client;
            Method = method;
            Headers = new Headers();
            iresource = resource;
            IsWebSocketUpgrade = Client.WebSocket?.Status == WebSocketStatus.Waiting;
            CachedProtocolProvider = ProtocolController.ResolveProtocolProvider(protocolIdentifier);
        }

        internal RequestParameters(ITraceable trace, Methods method, ref string uri, byte[] body, Headers headers)
        {
            TraceId = trace.TraceId;
            Client = trace.Client;
            Method = method;
            Headers = headers ?? new Headers();
            IsWebSocketUpgrade = Client.WebSocket?.Status == WebSocketStatus.Waiting;

            Uri = URI.ParseInternal(ref uri, PercentCharsEscaped(headers), trace, out var key, out var cachedProtocolProvider);
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