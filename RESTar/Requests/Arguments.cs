using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Linq;
using RESTar.OData;
using RESTar.Results.Error;
using static System.Text.RegularExpressions.RegexOptions;
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
        public Origin Origin { get; }
        public byte[] BodyBytes { get; set; }
        public Headers Headers { get; }
        public MimeType ContentType { get; set; }
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

        internal Arguments(Action action, string query, byte[] body = null, Dictionary<string, string> headers = null, Origin origin = null)
        {
            Action = action;
            Uri = URI.ParseInternal(query, PercentCharsEscaped(headers), out var protocol);
            BodyBytes = body;
            Headers = new Headers(headers);
            Origin = origin;
            ContentType = MimeType.Parse(Headers.SafeGet("Content-Type"));
            Accept = MimeType.ParseMany(Headers.SafeGet("Accept"));
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