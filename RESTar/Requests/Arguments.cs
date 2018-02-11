using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Logging;
using RESTar.Results.Error;
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

    /// <summary>
    /// A RESTar class that defines the arguments that are used when creating a RESTar request to evaluate 
    /// an incoming call. Arguments is a unified way to talk about the input to request evaluation, 
    /// regardless of protocol and web technologies.
    /// </summary>
    public class Arguments : ILogable, ITraceable
    {
        /// <summary>
        /// The action to perform
        /// </summary>
        public Action Action { get; }

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
                BodyBytes = BodyBytes ?? value?.Macro?.BodyBinary.ToArray();
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
        public byte[] BodyBytes { get; private set; }

        /// <inheritdoc />
        public Headers Headers { get; }

        /// <summary>
        /// The content type registered in the Content-Type header
        /// </summary>
        public MimeType ContentType { get; }

        /// <summary>
        /// The content type registered in the Accept header
        /// </summary>
        public MimeType Accept { get; }

        internal ResultFinalizer ResultFinalizer { get; }
        internal string AuthToken { get; set; }
        internal Exception Error { get; set; }

        /// <inheritdoc />
        public string TraceId { get; }

        /// <inheritdoc />
        public bool ExcludeHeaders => IResource is IEntityResource e && e.RequiresAuthentication;

        LogEventType ILogable.LogEventType { get; } = LogEventType.HttpInput;
        string ILogable.LogMessage => $"{Action} {UnparsedUri}{(BodyBytes?.Length > 0 ? $" ({BodyBytes.Length} bytes)" : "")}";
        private string _contentString;

        string ILogable.LogContent
        {
            get
            {
                if (BodyBytes == null) return null;
                return _contentString ?? (_contentString = Encoding.UTF8.GetString(BodyBytes));
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

        internal Arguments(Action action, ref string query, byte[] body, Headers headers, TCPConnection tcpConnection)
        {
            TraceId = tcpConnection.TraceId;
            Action = action;
            Headers = headers ?? new Headers();
            BodyBytes = body;
            Uri = URI.ParseInternal(ref query, PercentCharsEscaped(headers), out var key);
            if (key != null)
                Headers["Authorization"] = $"apikey {UnpackUriKey(key)}";
            UnparsedUri = query;
            TcpConnection = tcpConnection;
            ContentType = MimeType.Parse(Headers["Content-Type"]);
            Accept = MimeType.ParseMany(Headers["Accept"]);
            if (Uri.Protocol != null)
                ResultFinalizer = Uri.Protocol.FinalizeResult;
            try
            {
                Uri.Protocol?.CheckCompliance(Headers);
            }
            catch (NotImplementedException) { }
            catch (Exception e)
            {
                Error = e;
                return;
            }
            if (ContentType.TypeCode == MimeTypeCode.Unsupported && action < Action.OPTIONS)
                Error = new UnsupportedContent(ContentType);
            if (Accept.TypeCode == MimeTypeCode.Unsupported)
                Error = new NotAcceptable(Accept);
            if (uri.HasError)
                Error = uri.Error;
        }
    }
}