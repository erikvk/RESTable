using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RESTar
{
    internal class HttpRequest
    {
        internal RESTarMethods Method { get; private set; }
        internal Uri URI { get; private set; }
        internal Dictionary<string, string> Headers { get; }
        internal string Accept;
        internal string ContentType;
        internal bool Internal { get; private set; }

        private HttpRequest()
        {
            Headers = new Dictionary<string, string>();
        }

        internal static HttpRequest Parse(string uriString)
        {
            var r = new HttpRequest();
            uriString.Trim().Split(new[] {' '}, 3).ForEach((part, index) =>
            {
                switch (index)
                {
                    case 0:
                        RESTarMethods method;
                        if (!Enum.TryParse(part, true, out method))
                            throw new Exception($"Invalid method '{part}'");
                        r.Method = method;
                        break;
                    case 1:
                        if (!part.StartsWith("/"))
                        {
                            r.Internal = false;
                            Uri uri;
                            if (!Uri.TryCreate(part, UriKind.Absolute, out uri))
                                throw new Exception($"Invalid uri '{part}'");
                            r.URI = uri;
                        }
                        else
                        {
                            r.Internal = true;
                            Uri uri;
                            if (!Uri.TryCreate(part, UriKind.Relative, out uri))
                                throw new Exception($"Invalid uri '{part}'");
                            r.URI = uri;
                        }
                        break;
                    case 2:
                        var regex = new Regex(@"\[(?<header>.+):[\s]*(?<value>.+)\]");
                        var matches = regex.Matches(part);
                        foreach (Match match in matches)
                        {
                            var header = match.Groups["header"].ToString();
                            var value = match.Groups["value"].ToString();
                            switch (header.ToLower())
                            {
                                case "accept":
                                    r.Accept = value;
                                    break;
                                case "content-type":
                                    r.ContentType = value;
                                    break;
                                default:
                                    r.Headers[header] = value;
                                    break;
                            }
                        }
                        break;
                }
            });
            return r;
        }
    }
}