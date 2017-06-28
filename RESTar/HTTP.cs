using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Starcounter;

namespace RESTar
{
    internal static class HTTP
    {
        internal static Response InternalRequest
        (
            RESTarMethods method,
            Uri relativeUri,
            string authToken,
            byte[] bodyBytes = null,
            string contentType = null,
            string accept = null,
            Dictionary<string, string> headers = null,
            Action<Response> then = null
        )
        {
            try
            {
                headers = headers ?? new Dictionary<string, string>();
                if (contentType != null || accept != null)
                {
                    if (contentType != null)
                        headers["Content-Type"] = contentType;
                    if (accept != null)
                        headers["Accept"] = accept;
                }
                headers["RESTar-AuthToken"] = authToken;
                var response = Self.CustomRESTRequest
                (
                    method: method.ToString(),
                    uri: Settings._Uri + relativeUri,
                    body: null,
                    bodyBytes: bodyBytes,
                    headersDictionary: headers,
                    port: Settings._Port
                );
                then?.Invoke(response);
                return response;
            }
            catch
            {
                return null;
            }
        }

        internal static Response ExternalRequest
        (
            RESTarMethods method,
            Uri uri,
            byte[] bodyBytes = null,
            string contentType = null,
            string accept = null,
            Dictionary<string, string> headers = null,
            Action<Response> then = null
        )
        {
            try
            {
                if (uri.Scheme == Uri.UriSchemeHttps)
                    return HttpsRequest
                    (
                        method: method.ToString(),
                        uri: uri.ToString(),
                        bodyBytes: bodyBytes,
                        contentType: contentType,
                        accept: accept,
                        headers: headers,
                        then: then
                    );
                if (contentType != null || accept != null)
                {
                    headers = headers ?? new Dictionary<string, string>();
                    if (contentType != null)
                        headers["Content-Type"] = contentType;
                    if (accept != null)
                        headers["Accept"] = accept;
                }
                var response = Http.CustomRESTRequest
                (
                    method: method.ToString(),
                    uri: uri.ToString(),
                    bodyBytes: bodyBytes,
                    headersDictionary: headers
                );
                then?.Invoke(response);
                return response;
            }
            catch
            {
                return null;
            }
        }

        private static Response HttpsRequest(string method, string uri, byte[] bodyBytes = null,
            string contentType = null, string accept = null, IDictionary<string, string> headers = null,
            Action<Response> then = null)
        {
            try
            {
                var request = (HttpWebRequest) WebRequest.Create(uri);
                request.AllowAutoRedirect = false;
                request.Method = method;
                headers?.ForEach(pair => request.Headers[pair.Key] = pair.Value);
                request.ContentLength = bodyBytes?.Length ?? 0;
                if (contentType != null) request.ContentType = contentType;
                if (accept != null) request.Accept = accept;
                var response = (HttpWebResponse) request.GetResponse();
                var respLoc = response.Headers["Location"];
                if (response.StatusCode == HttpStatusCode.MovedPermanently && respLoc != null)
                    return HttpsRequest(method, respLoc, bodyBytes, contentType, accept, headers, then);
                var responseStream = response.GetResponseStream();
                if (responseStream == null) throw new NullReferenceException("ResponseStream was null");
                byte[] responseBody;
                using (var stream = new MemoryStream())
                {
                    responseStream.CopyTo(stream);
                    responseBody = stream.ToArray();
                }
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