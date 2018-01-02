using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RESTar.Protocols;
using RESTar.Admin;
using RESTar.Internal;

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

        private static readonly string DefaultResourceSpecifier = typeof(AvailableResource).FullName;

        internal static URI ParseInternal(string query, bool percentCharsEscaped, out RESTProtocols protocol)
        {
            var uri = new URI();
            if (percentCharsEscaped) query = query.Replace("%25", "%");
            Action<URI, string> populator;
            var groups = Regex.Match(query, RegEx.Protocol).Groups;
            var protocolString = groups["proto"].Value;
            var tail = groups["tail"].Value;
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
            var uri = ParseInternal(uriString, false, out var _);
            if (uri.HasError) throw uri.Error;
            return uri;
        }

        internal URI()
        {
            ResourceSpecifier = DefaultResourceSpecifier;
            Conditions = new List<UriCondition>();
            MetaConditions = new List<UriCondition>();
        }

        public string ToString(RESTProtocols protocol)
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