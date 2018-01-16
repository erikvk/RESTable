using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RESTar.Admin;
using RESTar.Internal;
using RESTar.OData;
using RESTar.Results.Fail.BadRequest;

namespace RESTar.Requests
{
    internal class URI : IUriParameters
    {
        public string ResourceSpecifier { get; internal set; }
        public string ViewName { get; internal set; }
        public List<UriCondition> Conditions { get; }
        public List<UriCondition> MetaConditions { get; }
        public DbMacro Macro { get; internal set; }
        internal Exception Error { get; private set; }
        internal bool HasError => Error != null;
        internal static readonly string DefaultResourceSpecifier = typeof(AvailableResource).FullName;
        internal static readonly string MetadataResourceSpecifier = typeof(Metadata).FullName;

        internal static URI ParseInternal(ref string query, bool percentCharsEscaped, out RESTProtocols protocol, out string key)
        {
            var uri = new URI();
            key = null;
            if (percentCharsEscaped) query = query.Replace("%25", "%");
            Action<URI, string> populator;
            var groups = Regex.Match(query, RegEx.Protocol).Groups;
            var protocolString = groups["proto"].Value;
            var _key = groups["key"].Value;
            var tail = groups["tail"].Value;
            if (_key.Length > 0)
            {
                key = _key;
                query = protocolString + tail;
            }
            switch (protocolString)
            {
                case "":
                case "-restar":
                    populator = RESTarProtocolProvider.PopulateUri;
                    protocol = RESTProtocols.RESTar;
                    break;
                case "-odata":
                    populator = ODataProtocolProvider.PopulateUri;
                    protocol = RESTProtocols.OData;
                    break;
                default:
                    protocol = default;
                    uri.Error = new InvalidSyntax(ErrorCodes.InvalidUriSyntax, $"Unknown protocol '{protocolString}'");
                    return uri;
            }
            try
            {
                populator(uri, tail);
            }
            catch (Exception e)
            {
                uri.Error = e;
            }
            return uri;
        }

        internal static URI Parse(string uriString)
        {
            var uri = ParseInternal(ref uriString, false, out var _, out var _);
            if (uri.HasError) throw uri.Error;
            return uri;
        }

        internal URI()
        {
            ResourceSpecifier = DefaultResourceSpecifier;
            Conditions = new List<UriCondition>();
            MetaConditions = new List<UriCondition>();
        }

        public string ToString(RESTProtocols protocol = RESTProtocols.RESTar)
        {
            switch (protocol)
            {
                case RESTProtocols.RESTar: return RESTarProtocolProvider.MakeRelativeUri(this);
                case RESTProtocols.OData: return ODataProtocolProvider.MakeRelativeUri(this);
                default: throw new ArgumentOutOfRangeException(nameof(protocol), protocol, null);
            }
        }
    }
}