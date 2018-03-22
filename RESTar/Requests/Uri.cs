using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RESTar.Admin;
using RESTar.Results.Error.NotFound;

namespace RESTar.Requests
{
    /// <inheritdoc />
    /// <summary>
    /// Encodes a URI that is used in a request
    /// </summary>
    public class URI : IUriComponents
    {
        /// <inheritdoc />
        public string ResourceSpecifier { get; set; }

        /// <inheritdoc />
        public string ViewName { get; set; }

        /// <inheritdoc />
        public List<UriCondition> Conditions { get; }

        /// <inheritdoc />
        public List<UriCondition> MetaConditions { get; }

        internal DbMacro Macro { get; set; }
        internal Exception Error { get; private set; }
        internal bool HasError => Error != null;
        private IProtocolProvider ProtocolProvider { get; set; }

        internal static URI ParseInternal(ref string uriString, bool percentCharsEscaped, ITraceable trace, out string key,
            out CachedProtocolProvider cachedProtocolProvider)
        {
            var uri = new URI();
            key = null;
            if (percentCharsEscaped) uriString = uriString.Replace("%25", "%");
            var groups = Regex.Match(uriString, RegEx.Protocol).Groups;
            var protocolString = groups["proto"].Value;
            var _key = groups["key"].Value;
            var tail = groups["tail"].Value;
            if (_key.Length > 0)
            {
                key = _key;
                uriString = protocolString + tail;
            }
            if (!RequestEvaluator.ProtocolProviders.TryGetValue(protocolString, out cachedProtocolProvider))
            {
                uri.Error = new UnknownProtocol(protocolString);
                return uri;
            }
            uri.ProtocolProvider = cachedProtocolProvider.ProtocolProvider;
            try
            {
                cachedProtocolProvider.ProtocolProvider.ParseQuery(tail, uri, trace.Client);
            }
            catch (Exception e)
            {
                uri.Error = e;
            }
            return uri;
        }

        internal static URI Parse(string uriString)
        {
            var uri = ParseInternal(ref uriString, false, Client.Internal, out var _, out var _);
            if (uri.HasError) throw uri.Error;
            return uri;
        }

        private URI()
        {
            Conditions = new List<UriCondition>();
            MetaConditions = new List<UriCondition>();
        }

        /// <inheritdoc />
        public override string ToString() => ProtocolProvider?.MakeRelativeUri(this) ?? "";
    }
}