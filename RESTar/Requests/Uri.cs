using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RESTar.Admin;
using RESTar.Internal;
using RESTar.Results.Error.BadRequest;

namespace RESTar.Requests
{
    /// <inheritdoc />
    /// <summary>
    /// Encodes a URI that is used in a request
    /// </summary>
    public class URI : IUriParameters
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
        internal IProtocolProvider Protocol { get; private set; }

        internal static URI ParseInternal(ref string query, bool percentCharsEscaped, out string key)
        {
            var uri = new URI();
            key = null;
            if (percentCharsEscaped) query = query.Replace("%25", "%");
            var groups = Regex.Match(query, RegEx.Protocol).Groups;
            var protocolString = groups["proto"].Value;
            var _key = groups["key"].Value;
            var tail = groups["tail"].Value;
            if (_key.Length > 0)
            {
                key = _key;
                query = protocolString + tail;
            }
            uri.Protocol = RequestEvaluator.ProtocolProviders.SafeGet(protocolString);
            if (uri.Protocol == null)
            {
                uri.Error = new InvalidSyntax(ErrorCodes.InvalidUriSyntax, $"Could not identify any protocol by '{protocolString}'");
                return uri;
            }
            try
            {
                uri.Protocol.ParseQuery(tail, uri);
            }
            catch (Exception e)
            {
                uri.Error = e;
            }
            return uri;
        }

        internal static URI Parse(string uriString)
        {
            var uri = ParseInternal(ref uriString, false, out var _);
            if (uri.HasError) throw uri.Error;
            return uri;
        }

        internal URI()
        {
            Conditions = new List<UriCondition>();
            MetaConditions = new List<UriCondition>();
        }

        /// <inheritdoc />
        public override string ToString() => Protocol?.MakeRelativeUri(this) ?? "";
    }
}