using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using RESTar.Linq;
using RESTar.Queries;
using RESTar.Results.Error;

namespace RESTar.Internal
{
    internal class HttpRequest : ITraceable
    {
        internal Method Method { get; private set; }
        internal string URI { get; private set; }
        private Dictionary<string, string> Headers { get; }
        internal string Accept;
        internal string ContentType;
        private bool IsInternal { get; set; }
        private static readonly Regex HeaderRegex = new Regex(RegEx.RequestHeader);
        internal Stream Body;

        public string TraceId { get; }
        public Context Context { get; }

        internal ISerializedResult GetResponse()
        {
            if (IsInternal)
            {
                Headers["Content-Type"] = ContentType;
                Headers["Accept"] = Accept;
                var uri = URI;
                return Query
                    .Create(this, Method, ref uri, Body.ToByteArray(), new Headers(Headers))
                    .Result                    .Serialize();
            }
            return MakeExternalRequest(this, Method.ToString(), new Uri(URI), Body, ContentType, Accept, Headers);
        }

        internal HttpRequest(ITraceable trace, string uriString)
        {
            TraceId = trace.TraceId;
            Context = trace.Context;
            Headers = new Dictionary<string, string>();
            uriString.Trim().Split(new[] {' '}, 3).ForEach((part, index) =>
            {
                switch (index)
                {
                    case 0:
                        if (!Enum.TryParse(part, true, out Method method))
                            throw new HttpRequestException("Invalid or missing method");
                        Method = method;
                        break;
                    case 1:
                        if (!part.StartsWith("/"))
                        {
                            IsInternal = false;
                            URI = part;
                        }
                        else
                        {
                            IsInternal = true;
                            URI = part;
                        }
                        break;
                    case 2:
                        var matches = HeaderRegex.Matches(part);
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

        private static HttpResponse MakeExternalRequest(ITraceable trace, string method, Uri uri, Stream body = null, string contentType = null,
            string accept = null, IDictionary<string, string> headers = null)
        {
            try
            {
                var request = (HttpWebRequest) WebRequest.Create(uri);
                request.AllowAutoRedirect = false;
                request.Method = method;
                headers?.ForEach(pair => request.Headers[pair.Key] = pair.Value);
                if (contentType != null) request.ContentType = contentType;
                if (accept != null) request.Accept = accept;
                if (body != null)
                {
                    request.ContentLength = body.Length;
                    using (var requestStream = request.GetRequestStreamAsync().Result)
                    using (body)
                        body.CopyTo(requestStream);
                }
                var webResponse = (HttpWebResponse) request.GetResponseAsync().Result;
                var respLoc = webResponse.Headers["Location"];
                if (webResponse.StatusCode == HttpStatusCode.MovedPermanently && respLoc != null)
                    return MakeExternalRequest(trace, method, new Uri(respLoc), body, contentType, accept, headers);
                return new HttpResponse(trace, webResponse);
            }
            catch (WebException we)
            {
                Log.Warn($"!!! HTTP {method} Error at {uri} : {we.Message}");
                if (!(we.Response is HttpWebResponse response)) return null;
                var _response = new HttpResponse(trace, response.StatusCode, response.StatusDescription);
                foreach (var header in response.Headers.AllKeys)
                    _response.Headers[header] = response.Headers[header];
                return _response;
            }
            catch (Exception e)
            {
                Log.Warn($"!!! HTTP {method} Error at {uri} : {e.Message}");
                return new HttpResponse(trace, HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }
}