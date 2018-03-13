using System;
using System.Collections.Generic;
using System.Text;
using RESTar.Logging;
using RESTar.Requests;
using System.Web;
using RESTar.Internal;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    public class NewRequest<TResource> : NewRequest<TResource, TResource> where TResource : class
    {

    }

    public class NewRequest<TResource, TEntityType> : IDisposable, ILogable, ITraceable where TResource : class where TEntityType : class
    {
        internal string AuthToken { get; set; }

        /// <summary>
        /// The method to perform
        /// </summary>
        public Methods Method { get; }

        private string UnparsedUri { get; }

        /// <summary>
        /// The URI contained in the arguments
        /// </summary>
        public URI Uri { get; }

        private IResource iresource;
        internal IResource IResource => iresource ?? (iresource = Resource.Find(Uri.ResourceSpecifier));

        /// <inheritdoc />
        public TCPConnection TcpConnection { get; }

        /// <summary>
        /// The body as byte array
        /// </summary>
        public Body Body { get; }

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


        public static NewRequest<TResource, TEntityType> Create(ITraceable trace, Methods method, string uri, byte[] body = null, Headers headers = null)
        {
            return new NewRequest<T>(() => { return RequestEvaluator.Evaluate(trace, method, ref uri, body, headers).GetRawResult(); });
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}