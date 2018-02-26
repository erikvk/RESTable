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
    public interface IUriParameters
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
    /// A RESTar class that defines the arguments that are used when creating a RESTar request to evaluate 
    /// an incoming call. Arguments is a unified way to talk about the input to request evaluation, 
    /// regardless of protocol and web technologies.
    /// </summary>
    public class Context : ILogable, ITraceable
    {
        /// <summary>
        /// The method to perform
        /// </summary>
        public Methods Method { get; }

        private URI uri;
        private string UnparsedUri { get; }

        /// <summary>
        /// The URI contained in the arguments
        /// </summary>
        public URI Uri
        {
            get => uri;
            private set
            {
                Body = Body.HasContent ? Body : new Body(value?.Macro?.BodyBinary.ToArray(), "application/json", Serializers.Json);
                value?.Macro?.HeadersDictionary?.ForEach(pair =>
                {
                    var currentValue = Headers.SafeGet(pair.Key);
                    if (string.IsNullOrWhiteSpace(currentValue) || currentValue == "*/*")
                        Headers[pair.Key] = pair.Value;
                });
                uri = value;
            }
        }

        private IResource iresource;
        internal IResource IResource => iresource ?? (iresource = Resource.Find(Uri.ResourceSpecifier));

        /// <inheritdoc />
        public TCPConnection TcpConnection { get; }

        /// <summary>
        /// The body as byte array
        /// </summary>
        public Body Body { get; private set; }

        /// <inheritdoc />
        public Headers Headers { get; }

        /// <summary>
        /// The content type registered in the Content-Type header
        /// </summary>
        public ContentType ContentType { get; }

        /// <summary>
        /// The content type provider to use when deserializing input
        /// </summary>
        public IContentTypeProvider InputContentTypeProvider { get; }

        /// <summary>
        /// The content types registered in the Accept header
        /// </summary>
        public ContentType Accept { get; }

        /// <summary>
        /// The content type provider to use when serializing output
        /// </summary>
        public IContentTypeProvider OutputContentTypeProvider { get; }

        internal ResultFinalizer ResultFinalizer { get; }
        internal Exception Error { get; set; }

        internal CachedProtocolProvider CachedProtocolProvider { get; }

        /// <inheritdoc />
        public string TraceId { get; }

        /// <inheritdoc />
        public bool ExcludeHeaders => IResource is IEntityResource e && e.RequiresAuthentication;

        LogEventType ILogable.LogEventType { get; } = LogEventType.HttpInput;
        string ILogable.LogMessage => $"{Method} {UnparsedUri}{(Body.HasContent ? $" ({Body.Bytes.Length} bytes)" : "")}";
        private string _contentString;

        string ILogable.LogContent
        {
            get
            {
                if (!Body.HasContent) return null;
                return _contentString ?? (_contentString = Encoding.UTF8.GetString(Body.Bytes));
            }
        }

        /// <inheritdoc />
        public string HeadersStringCache { get; set; }

        internal void ThrowIfError()
        {
            if (Error != null) throw Error;
        }

        private static bool PercentCharsEscaped(IDictionary<string, string> headers)
        {
            return headers?.ContainsKey("X-ARR-LOG-ID") == true;
        }

        private static string UnpackUriKey(string uriKey)
        {
            return uriKey != null ? HttpUtility.UrlDecode(uriKey).Substring(1, uriKey.Length - 2) : null;
        }

        internal Context(Methods method, ref string uri, byte[] body, Headers headers, ITraceable trace)
        {
            TraceId = trace.TraceId;
            Method = method;
            Headers = headers ?? new Headers();
            Uri = URI.ParseInternal(ref uri, PercentCharsEscaped(headers), out var key, out var cachedProtocolProvider);
            CachedProtocolProvider = cachedProtocolProvider;
            if (key != null)
                Headers["Authorization"] = $"apikey {UnpackUriKey(key)}";
            UnparsedUri = uri;
            TcpConnection = trace.TcpConnection;
            var contentType = ContentType.ParseInput(Headers["Content-Type"]);
            var accepts = ContentType.ParseManyOutput(Headers["Accept"]);

            if (cachedProtocolProvider != null)
            {
                ResultFinalizer = cachedProtocolProvider.ProtocolProvider.FinalizeResult;
                if (contentType.MimeType == null)
                {
                    ContentType = cachedProtocolProvider.DefaultInputContentType;
                    InputContentTypeProvider = cachedProtocolProvider.DefaultInputProvider;
                }
                else
                {
                    if (!cachedProtocolProvider.InputContentTypeProviders.TryGetValue(contentType.MimeType, out var provider))
                        Error = new UnsupportedContent(Headers["Content-Type"]);
                    else
                    {
                        ContentType = contentType;
                        InputContentTypeProvider = provider;
                    }
                }

                if (accepts == null)
                {
                    Accept = cachedProtocolProvider.DefaultOutputContentType;
                    OutputContentTypeProvider = cachedProtocolProvider.DefaultOutputProvider;
                }
                else
                {
                    IContentTypeProvider acceptProvider = null;
                    var containedWildcard = false;
                    var accept = accepts.FirstOrDefault(a =>
                    {
                        if (a.MimeType == "*/*")
                        {
                            containedWildcard = true;
                            return false;
                        }
                        return cachedProtocolProvider.OutputContentTypeProviders.TryGetValue(a.MimeType, out acceptProvider);
                    });
                    if (acceptProvider == null)
                    {
                        if (containedWildcard)
                        {
                            Accept = cachedProtocolProvider.DefaultOutputContentType;
                            OutputContentTypeProvider = cachedProtocolProvider.DefaultOutputProvider;
                        }
                        else Error = new NotAcceptable(Headers["Accept"]);
                    }
                    else
                    {
                        Accept = accept;
                        OutputContentTypeProvider = acceptProvider;
                    }
                }
            }
            Body = new Body(body, ContentType, InputContentTypeProvider);
            try
            {
                cachedProtocolProvider?.ProtocolProvider.CheckCompliance(this);
            }
            catch (NotImplementedException) { }
            catch (Exception e)
            {
                Error = e;
                return;
            }
            if (this.uri.HasError)
                Error = this.uri.Error;
        }
    }
}