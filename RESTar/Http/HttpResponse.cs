using System.IO;
using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Requests;
using Starcounter;
using static System.StringSplitOptions;

namespace RESTar.Http
{
    internal class HttpResponse : IFinalizedResult
    {
        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public long ContentLength { get; set; }
        public string ContentType { get; private set; }
        public Stream Body { get; private set; }
        public Headers Headers { get; private set; }
        public ICollection<string> Cookies { get; private set; }
        public bool HasContent => ContentLength > 0;
        internal bool IsSuccessStatusCode => StatusCode >= (HttpStatusCode) 200 && StatusCode < (HttpStatusCode) 300;

        public static explicit operator HttpResponse(HttpWebResponse webResponse)
        {
            var response = new HttpResponse
            {
                StatusCode = webResponse.StatusCode,
                StatusDescription = webResponse.StatusDescription,
                ContentLength = webResponse.ContentLength,
                ContentType = webResponse.ContentType,
                Body = webResponse.GetResponseStream() ?? throw new NullReferenceException("ResponseStream was null"),
                Headers = new Headers(),
                Cookies = new List<string>()
            };
            foreach (var header in webResponse.Headers.AllKeys)
                response.Headers[header] = webResponse.Headers[header];
            return response;
        }

        public static explicit operator HttpResponse(Response scResponse)
        {
            var response = new HttpResponse
            {
                StatusCode = (HttpStatusCode) scResponse.StatusCode,
                StatusDescription = scResponse.StatusDescription,
                ContentLength = scResponse.ContentLength,
                ContentType = scResponse.ContentType,
                Body = scResponse.StreamedBody,
                Headers = new Headers()
            };
            scResponse.GetAllHeaders()
                .Split("\r\n", RemoveEmptyEntries)
                .Select(s => s.TSplit(':'))
                .ForEach(tuple =>
                {
                    var (name, value) = tuple;
                    if (name.EqualsNoCase("Set-Cookie"))
                        response.Cookies.Add(value.Trim());
                    else response.Headers[name] = value.Trim();
                });
            return response;
        }
    }
}