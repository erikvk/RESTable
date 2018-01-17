using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.OData;
using RESTar.Results.Error;
using static System.Text.RegularExpressions.RegexOptions;

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

        /// <summary>
        /// Writes the URI parameters to an URI string
        /// </summary>
        string ToString(RESTProtocols protocol = RESTProtocols.RESTar);
    }

    /// <summary>
    /// A RESTar class that defines the arguments that are used when creating a RESTar request to evaluate 
    /// an incoming call. Arguments is a unified way to talk about the input to request evaluation, 
    /// regardless of protocol and web technologies.
    /// </summary>
    internal class Arguments
    {
        public Action Action { get; }
        private URI uri;

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

        public IResource IResource => Resource.Find(Uri.ResourceSpecifier);
        public TCPConnection TcpConnection { get; }
        public byte[] BodyBytes { get; private set; }
        public Headers Headers { get; }
        public MimeType ContentType { get; }
        public MimeType Accept { get; }
        public ResultFinalizer ResultFinalizer { get; }
        internal string AuthToken { get; set; }
        internal Exception Error { get; set; }

        public void ThrowIfError()
        {
            if (Error != null) throw Error;
        }

        internal IEnumerable<KeyValuePair<string, string>> CustomHeaders => Headers
            .Where(h => !Regex.IsMatch(h.Key, RegEx.ReservedHeaders, IgnoreCase));

        private static bool PercentCharsEscaped(IDictionary<string, string> headers)
        {
            return headers?.ContainsKey("X-ARR-LOG-ID") == true;
        }

        private static string UnpackUriKey(string uriKey)
        {
            return uriKey != null ? HttpUtility.UrlDecode(uriKey).Substring(1, uriKey.Length - 2) : null;
        }

        internal Arguments(Action action, ref string query, byte[] body = null, Headers headers = null, TCPConnection tcpConnection = null)
        {
            Action = action;
            Headers = headers ?? new Headers();
            Uri = URI.ParseInternal(ref query, PercentCharsEscaped(headers), out var protocol, out var key);
            if (key != null)
                Headers["Authorization"] = $"apikey {UnpackUriKey(key)}";
            BodyBytes = body;
            TcpConnection = tcpConnection;
            ContentType = MimeType.Parse(Headers["Content-Type"]);
            Accept = MimeType.ParseMany(Headers["Accept"]);
            switch (protocol)
            {
                case RESTProtocols.RESTar:
                    ResultFinalizer = RESTarProtocolProvider.FinalizeResult;
                    break;
                case RESTProtocols.OData:
                    if (!ODataProtocolProvider.IsCompliant(this, out var odataError))
                    {
                        Error = odataError;
                        return;
                    }
                    ResultFinalizer = ODataProtocolProvider.FinalizeResult;
                    break;
            }
            if (ContentType.TypeCode == MimeTypeCode.Unsupported)
                Error = new UnsupportedContent(ContentType);
            if (Accept.TypeCode == MimeTypeCode.Unsupported)
                Error = new NotAcceptable(Accept);
            if (uri.HasError)
                Error = uri.Error;
        }
    }
}