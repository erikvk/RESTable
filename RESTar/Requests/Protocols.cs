using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RESTar.Requests
{
    internal enum Protocols
    {
        RESTar,
        OData
    }

    internal static class Protocol
    {
        internal static RequestArguments MakeArguments
        (
            string uri,
            byte[] body,
            Dictionary<string, string> headers,
            string contentType,
            string accept,
            Origin origin
        )
        {
            headers = headers ?? new Dictionary<string, string>();
            var protocol = headers.TryGetNoCase("odata-version", out var v) && v == "4.0"
                ? Protocols.OData
                : Protocols.RESTar;
            switch (protocol)
            {
                case Protocols.RESTar: return RESTar.Make(uri, body, headers, contentType, accept, origin);
                case Protocols.OData: return OData.Make(uri, body, headers, contentType, accept, origin);
                default: return null;
            }
        }

        private static class RESTar
        {
            internal static RequestArguments Make(string uri, byte[] body, IDictionary<string, string> headers, string contentType,
                string accept, Origin origin)
            {
                var arrProxy = headers.ContainsKey("X-ARR-LOG-ID");
                var args = new RequestArguments(uri, arrProxy)
                {
                    Origin = origin,
                    BodyBytes = body,
                    Headers = headers,
                    ContentType = contentType,
                    Accept = accept
                };
                if (args.Macro == null) return args;
                args.BodyBytes = args.BodyBytes ?? args.Macro.BodyBinary.ToArray();
                if (args.Macro.Headers == null) return args;
                foreach (var pair in JObject.Parse(args.Macro.Headers))
                {
                    var currentValue = args.Headers[pair.Key];
                    if (string.IsNullOrWhiteSpace(currentValue) || currentValue == "*/*")
                        args.Headers[pair.Key] = pair.Value.Value<string>();
                }
                return args;
            }
        }

        private static class OData
        {
            internal static RequestArguments Make(string uri, byte[] body, IDictionary<string, string> headers, string contentType,
                string accept, Origin origin)
            {
                var arrProxy = headers.ContainsKey("X-ARR-LOG-ID");
                var args = new RequestArguments
                {
                    Origin = origin,
                    BodyBytes = body,
                    Headers = headers,
                    ContentType = contentType,
                    Accept = accept
                };
                


                return args;
            }
        }
    }
}