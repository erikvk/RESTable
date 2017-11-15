using System.Collections.Generic;
using System.IO;
using System.Net;
using System;
using Starcounter;
using static System.StringSplitOptions;

namespace RESTar.Http
{
    internal class HttpResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public long ContentLength { get; set; }
        public string ContentType { get; set; }
        public Stream Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }

        internal bool IsSuccessStatusCode => StatusCode >= (HttpStatusCode) 200 &&
                                             StatusCode < (HttpStatusCode) 300;

        public static explicit operator Response(HttpResponse response)
        {
            var scResponse = new Response
            {
                StatusCode = (ushort) response.StatusCode,
                StatusDescription = response.StatusDescription
            };
            if (response.ContentType != null)
                scResponse.ContentType = response.ContentType;
            scResponse.SetHeadersDictionary(response.Headers);
            if (response.Body != null)
            {
                if (!response.Body.CanSeek)
                    scResponse.BodyBytes = response.Body.ToByteArray();
                else scResponse.StreamedBody = response.Body;
            }
            return scResponse;
        }

        public static explicit operator HttpResponse(HttpWebResponse webResponse)
        {
            var response = new HttpResponse
            {
                StatusCode = webResponse.StatusCode,
                StatusDescription = webResponse.StatusDescription,
                ContentLength = webResponse.ContentLength,
                ContentType = webResponse.ContentType,
                Body = webResponse.GetResponseStream()
                       ?? throw new NullReferenceException("ResponseStream was null"),
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