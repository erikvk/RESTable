using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Logging;
using RESTar.Results.Error;
using RESTar.Serialization;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Requests
{
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
        List<UriCondition> Conditions { get; }

        /// <summary>
        /// Specifies the meta-conditions for the request
        /// </summary>
        List<UriCondition> MetaConditions { get; }
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
        /// The body of the request
        /// </summary>
        public Body Body => new Body(BodyBytes, Headers.ContentType ?? Serializers.Json.ContentType);

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

        private byte[] BodyBytes { get; set; }
        internal CachedProtocolProvider CachedProtocolProvider { get; }
        private string UnparsedUri { get; }
        private IResource iresource;
        internal IResource IResource => iresource ?? (iresource = Resource.Find(Uri.ResourceSpecifier));
        internal Exception Error { get; set; }
        private static bool PercentCharsEscaped(IDictionary<string, string> headers) => headers?.ContainsKey("X-ARR-LOG-ID") == true;
        private static string UnpackUriKey(string uriKey) => uriKey != null ? HttpUtility.UrlDecode(uriKey).Substring(1, uriKey.Length - 2) : null;
        bool ILogable.ExcludeHeaders => IResource is IEntityResource e && e.RequiresAuthentication;
        LogEventType ILogable.LogEventType { get; } = LogEventType.HttpInput;
        string ILogable.LogMessage => $"{Method} {UnparsedUri}{(Body.HasContent ? $" ({Body.Bytes.Length} bytes)" : "")}";
        DateTime ILogable.LogTime { get; } = DateTime.Now;
        string ILogable.HeadersStringCache { get; set; }
        private string _contentString;

        string ILogable.LogContent
        {
            get
            {
                if (!Body.HasContent) return null;
                return _contentString ?? (_contentString = Encoding.UTF8.GetString(Body.Bytes));
            }
        }

        #endregion

        internal RequestParameters(ITraceable trace, Methods method, IResource resource, string protocolId = null)
        {
            TraceId = trace.TraceId;
            Client = trace.Client;
            Method = method;
            Headers = new Headers();
            IsWebSocketUpgrade = Client.WebSocket?.Status == WebSocketStatus.Waiting;
            if (protocolId != null && RequestEvaluator.ProtocolProviders.TryGetValue(protocolId, out var provider))
                CachedProtocolProvider = provider;
            else CachedProtocolProvider = RequestEvaluator.DefaultProtocolProvider;
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

            try
            {
                cachedProtocolProvider?.ProtocolProvider.CheckCompliance(this);
            }
            catch (NotImplementedException) { }
            catch (Exception e)
            {
                Error = e;
            }
            if (Error == null && Uri?.HasError == true)
                Error = Uri.Error;
        }
    }
}