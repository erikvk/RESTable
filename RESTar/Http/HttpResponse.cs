using System.Collections.Generic;
using System.IO;
using System.Net;
using System;
using RESTar.Operations;
using Starcounter;
using static System.StringSplitOptions;

namespace RESTar.Http
{
    internal class HttpResponse : IFinalizedResult
    {
        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public long ContentLength { get; set; }
        public string ContentType { get; set; }
        public Stream Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }
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
                Headers = new Dictionary<string, string>()
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
                Headers = new Dictionary<string, string>()
            };
            foreach (var header in scResponse.GetAllHeaders().Split(new[] {"\r\n"}, RemoveEmptyEntries))
            {
                var name_value = header.Split(':');
                response.Headers[name_value[0]] = name_value.SafeGet(v => v[1].Trim());
            }

            return response;
        }
    }
}