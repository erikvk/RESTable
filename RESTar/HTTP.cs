using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using RESTar.Linq;
using Starcounter;

namespace RESTar
{
    /// <summary>
    /// Provides easy methods for making internal or external HTTP and external
    /// HTTPS calls.
    /// </summary>
    public static class HTTP
    {
        /// <summary>
        /// Makes an internal request. Make sure to include the original Request's
        /// AuthToken if you're sending internal RESTar requests.
        /// </summary>
        public static Response InternalRequest
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

        /// <summary>
        /// Makes an external HTTP or HTTPS request
        /// </summary>
        public static Response ExternalRequest
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
                if (bodyBytes != null)
                    using (var stream = request.GetRequestStream())
                        stream.Write(bodyBytes, 0, bodyBytes.Length);
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
                    StatusCode = (ushort) response.StatusCode,
                    StatusDescription = response.StatusDescription,
                    ContentLength = (int) response.ContentLength,
                    ContentType = accept ?? MimeTypes.JSON
                };
                foreach (var header in response.Headers.AllKeys)
                    _response.Headers[header] = response.Headers[header];
                switch (accept)
                {
                    case MimeTypes.Excel:
                        _response.BodyBytes = responseBody;
                        break;
                    default:
                        _response.Body = Encoding.UTF8.GetString(responseBody);
                        break;
                }
                then?.Invoke(_response);
                return _response;
            }
            catch (WebException we)
            {
                Log.Warn($"!!! HTTPS {method} Error at {uri} : {we.Message}");
                var response = we.Response as HttpWebResponse;
                if (response == null) return null;
                var _response = new Response
                {
                    StatusCode = (ushort) response.StatusCode,
                    StatusDescription = response.StatusDescription
                };
                foreach (var header in response.Headers.AllKeys)
                    _response.Headers[header] = response.Headers[header];
                return _response;
            }
            catch (Exception e)
            {
                Log.Warn($"!!! HTTPS {method} Error at {uri} : {e.Message}");
                return null;
            }
        }
    }
}