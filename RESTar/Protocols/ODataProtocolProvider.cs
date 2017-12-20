using System.Collections.Generic;
using System.Text.RegularExpressions;
using RESTar.Operations;
using RESTar.Requests;
using static RESTar.Internal.ErrorCodes;

namespace RESTar.Protocols
{
    internal class ODataProtocolProvider : IProtocolProvider
    {
        internal static bool HasODataHeader(Dictionary<string, string> headers)
        {
            return headers != null && (headers.TryGetNoCase("odata-version", out var v) ||
                                       headers.TryGetNoCase("odata-maxversion", out v)) && v == "4.0";
        }

        private static void PopulateFromUri(Arguments args, string uri)
        {
            var match = Regex.Match(uri, RegEx.ODataRequestUri);
            if (!match.Success) throw new SyntaxException(InvalidUriSyntax, "Check URI syntax");
            var entitySet = match.Groups["entityset"].Value.TrimStart('/');
            var options = match.Groups["options"].Value.TrimStart('-');

        }

        public IFinalizedResult FinalizeResult(Result result)
        {
            return null;
        }

        internal enum MetaDataLevel
        {
            None,
            Minimal,
            All
        }

        public Arguments MakeRequestArguments(string uri, byte[] body, IDictionary<string, string> headers,
            string contentType, string accept)
        {
            var args = new Arguments
            {
                BodyBytes = body,
                Headers = headers,
                ContentType = contentType,
                Accept = accept
            };
            PopulateFromUri(args, uri);
            return args;
        }
    }
}