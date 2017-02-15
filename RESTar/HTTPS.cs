using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using ClosedXML.Excel;
using Starcounter;

namespace RESTar
{
    internal static class HTTPS
    {
        internal static Response Request(string method, string uri, string bodyString = null, string contentType = null,
            string accept = null, IDictionary<string, string> headers = null, string log = null,
            Action<Response> then = null)
        {
            try
            {
                var request = (HttpWebRequest) WebRequest.Create(uri);
                request.AllowAutoRedirect = false;
                request.Method = method;
                headers?.ForEach(pair => request.Headers[pair.Key] = pair.Value);
                if (bodyString != null)
                {
                    var bodyBytes = Encoding.UTF8.GetBytes(bodyString);
                    request.ContentLength = bodyBytes.Length;
                    using (var stream = request.GetRequestStream())
                        stream.Write(bodyBytes, 0, bodyBytes.Length);
                }
                if (contentType != null) request.ContentType = contentType;
                if (accept != null) request.Accept = accept;
                var response = (HttpWebResponse) request.GetResponse();
                var respLoc = response.Headers["Location"];
                if (response.StatusCode == HttpStatusCode.MovedPermanently && respLoc != null)
                    return Request(method, respLoc, bodyString, contentType, accept, headers, log, then);
                var responseStream = response.GetResponseStream();
                if (responseStream == null) throw new NullReferenceException("ResponseStream was null");
                byte[] responseBody;
                using (var stream = new MemoryStream())
                {
                    responseStream.CopyTo(stream);
                    responseBody = stream.ToArray();
                }
                if (log != null) Log.Info(log);
                var _response = new Response
                {
                    BodyBytes = responseBody,
                    Body = Encoding.UTF8.GetString(responseBody),
                    ContentType = response.ContentType,
                    ContentLength = (int) response.ContentLength
                };
                then?.Invoke(_response);
                return _response;
            }
            catch (Exception e)
            {
                Log.Warn($"!!! HTTPS POST Error at {uri} : {e.Message}");
                return null;
            }
        }
    }
}