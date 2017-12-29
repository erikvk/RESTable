using System.Collections.Generic;
using System.Text.RegularExpressions;
using RESTar.Internal;
using RESTar.Requests;

namespace RESTar.Protocols
{
    internal static class Protocol
    {
        private static readonly RESTarProtocolProvider RESTarProvider;
        private static readonly ODataProtocolProvider ODataProvider;
        internal static int UriLength { private get; set; }

        static Protocol()
        {
            RESTarProvider = new RESTarProtocolProvider();
            ODataProvider = new ODataProtocolProvider();
        }

        private static bool PercentCharsEscaped(IDictionary<string, string> headers)
        {
            return headers?.ContainsKey("X-ARR-LOG-ID") == true;
        }

        internal static Arguments MakeArguments(string uri, byte[] body = null, Dictionary<string, string> headers = null,
            MimeType contentType = null, MimeType[] accept = null, Origin origin = null)
        {
            var groups = Regex.Match(uri, RegEx.Protocol).Groups;
            uri = groups["tail"].Value;
            if (PercentCharsEscaped(headers)) uri = uri.Replace("%25", "%");
            switch (groups["protocol"].Value)
            {
                case "":
                case null:
                case "-restar": return RESTarProvider.MakeRequestArguments(uri, body, headers, contentType, accept, origin);
                case "-odata": return ODataProvider.MakeRequestArguments(uri, body, headers, contentType, accept, origin);
                case var unknown: throw new InvalidSyntax(ErrorCodes.InvalidUriSyntax, $"Unknown protocol '{unknown}'");
            }
        }
    }
}