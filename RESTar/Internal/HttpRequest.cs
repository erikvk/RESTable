using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Results.Error;

namespace RESTar.Internal
{
    internal class HttpRequest : ITraceable
    {
        public Method Method { get; }
        public string URI { get; }
        public Headers Headers { get; }
        public Stream Body;
        public string TraceId { get; }
        public Context Context { get; }
        public HttpResponse GetResponse() => MakeExternalRequest(this, Method.ToString(), new Uri(URI), Body, Headers);

        internal HttpRequest(ITraceable trace, HeaderRequestParameters parameters, Stream body)
        {
            TraceId = trace.TraceId;
            Context = trace.Context;
            URI = parameters.URI;
            Body = body;
            Headers = parameters.Headers;
            Method = parameters.Method;
        }

        private static HttpResponse MakeExternalRequest(ITraceable trace, string method, Uri uri, Stream body, Headers headers)
        {
            try
            {
                var request = (HttpWebRequest) WebRequest.Create(uri);
                request.AllowAutoRedirect = false;
                request.Method = method;
                headers.Where(pair => pair.Key != "Content-Type" && pair.Key != "Accept")
                    .ForEach(pair => request.Headers[pair.Key] = pair.Value);
                if (headers.ContentType != null) request.ContentType = headers.ContentType.ToString();
                if (headers.Accept != null) request.Accept = headers.Accept.ToString();
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
                    return MakeExternalRequest(trace, method, new Uri(respLoc), body, headers);
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