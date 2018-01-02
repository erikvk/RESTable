using System;
using System.Collections.Generic;
using RESTar.Protocols;
using System.Text.RegularExpressions;
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
            var match = Regex.Match(query, RegEx.Protocol);
            if (!match.Success)
            {
                uri.Error = new InvalidSyntax(ErrorCodes.InvalidUriSyntax, "Invalid URI syntax");
                protocol = RESTProtocols.RESTar;
                return uri;
            }
            var groups = match.Groups;
            query = groups["tail"].Value;
            if (percentCharsEscaped) query = query.Replace("%25", "%");
            Action<URI, string> populator;
            switch (groups["protocol"].Value)
            {
                case "":
                case null:
                case "-restar":
                    populator = RESTarProtocolProvider.PopulateUri;
                    protocol = RESTProtocols.RESTar;
                    break;
                case "-odata":
                    populator = ODataProtocolProvider.PopulateUri;
                    protocol = RESTProtocols.OData;
                    break;
                case var unknown: throw new InvalidSyntax(ErrorCodes.InvalidUriSyntax, $"Unknown protocol '{unknown}'");
            }
            try
            {
                populator(uri, query);
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