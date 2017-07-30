using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RESTar.Linq;
using Starcounter;

namespace RESTar
{
    internal class HttpRequest
    {
        internal RESTarMethods Method { get; private set; }
        internal Uri URI { get; private set; }
        internal Dictionary<string, string> Headers { get; }
        internal string Accept;
        internal string ContentType;
        internal bool IsInternal { get; private set; }
        private static readonly Regex regex = new Regex(@"\[(?<header>.+):[\s]*(?<value>.+)\]");
        internal byte[] Bytes;
        internal string AuthToken;

        internal Response GetResponse() => IsInternal ? HTTP.Internal(this) : HTTP.External(this);

        internal HttpRequest(string uriString)
        {
            Headers = new Dictionary<string, string>();
            uriString.Trim().Split(new[] {' '}, 3).ForEach((part, index) =>
            {
                switch (index)
                {
                    case 0:
                        RESTarMethods method;
                        if (!Enum.TryParse(part, true, out method))
                            throw new HttpRequestException("Invalid or missing method");
                        Method = method;
                        break;
                    case 1:
                        if (!part.StartsWith("/"))
                        {
                            IsInternal = false;
                            if (!Uri.TryCreate(part, UriKind.Absolute, out var uri))
                                throw new HttpRequestException($"Invalid uri '{part}'");
                            URI = uri;
                        }
                        else
                        {
                            IsInternal = true;
                            if (!Uri.TryCreate(part, UriKind.Relative, out var uri))
                                throw new HttpRequestException($"Invalid uri '{part}'");
                            URI = uri;
                        }
                        break;
                    case 2:
                        var matches = regex.Matches(part);
                        if (matches.Count == 0) throw new HttpRequestException("Invalid header syntax");
                        foreach (Match match in matches)
                        {
                            var header = match.Groups["header"].ToString();
                            var value = match.Groups["value"].ToString();
                            switch (header.ToLower())
                            {
                                case "accept":
                                    Accept = value;
                                    break;
                                case "content-type":
                                    break;
                                default:
                                    Headers[header] = value;
                                    break;
                            }
                        }
                        break;
                }
            });
        }
    }
}