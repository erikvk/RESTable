using System.Collections.Generic;
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
            string contentType = null, string accept = null, Origin origin = null)
        {
            IProtocolProvider provider;
            if (ODataProtocolProvider.HasODataHeader(headers))
                provider = ODataProvider;
            else provider = RESTarProvider;
            if (PercentCharsEscaped(headers)) uri = uri.Replace("%25", "%");
            var args = provider.MakeRequestArguments(uri, body, headers, contentType, accept);
            args.ResultFinalizer = provider.FinalizeResult;
            args.Origin = origin ?? Origin.Internal;
            return args;
        }
    }
}