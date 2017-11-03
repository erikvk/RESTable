﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Operations;
using Starcounter;

namespace RESTar
{
    internal class HttpRequest
    {
        internal Methods Method { get; private set; }
        internal Uri URI { get; private set; }
        internal Dictionary<string, string> Headers { get; }
        internal string Accept;
        internal string ContentType;
        internal bool IsInternal { get; private set; }
        private static readonly Regex regex = new Regex(@"\[(?<header>.+):[\s]*(?<value>.+)\]");
        internal Stream Body;
        internal string AuthToken;

        internal Response GetResponse() => IsInternal ? Internal(this) : External(this);

        internal HttpRequest(string uriString)
        {
            Headers = new Dictionary<string, string>();
            uriString.Trim().Split(new[] {' '}, 3).ForEach((part, index) =>
            {
                switch (index)
                {
                    case 0:
                        if (!Enum.TryParse(part, true, out Methods method))
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

        private static Response Internal(HttpRequest request) => Internal
        (
            method: request.Method,
            relativeUri: request.URI,
            authToken: request.AuthToken,
            body: request.Body,
            contentType: request.ContentType,
            accept: request.Accept,
            headers: request.Headers
        );

        private static Response External(HttpRequest request) => External
        (
            method: request.Method,
            uri: request.URI,
            body: request.Body,
            contentType: request.ContentType,
            accept: request.Accept,
            headers: request.Headers
        );

        /// <summary>
        /// Makes an internal request. Make sure to include the original Request's
        /// AuthToken if you're sending internal RESTar requests.
        /// </summary>
        internal static Response Internal
        (
            Methods method,
            Uri relativeUri,
            string authToken,
            Stream body = null,
            string contentType = null,
            string accept = null,
            Dictionary<string, string> headers = null
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
                    bodyBytes: body?.ToByteArray(),
                    headersDictionary: headers,
                    port: Settings._Port
                );
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
        internal static Response External(Methods method, Uri uri, Stream body = null, string contentType = null,
            string accept = null, Dictionary<string, string> headers = null)
        {
            return Do.SafeGet(() => Request(method.ToString(), uri, body, contentType, accept, headers));
        }

        private static Response Request(string method, Uri uri, Stream body = null,
            string contentType = null, string accept = null, IDictionary<string, string> headers = null)
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
                    using (var requestStream = request.GetRequestStream())
                    using (body) body.CopyTo(requestStream);
                }
                var webResponse = (HttpWebResponse) request.GetResponse();
                var respLoc = webResponse.Headers["Location"];
                if (webResponse.StatusCode == HttpStatusCode.MovedPermanently && respLoc != null)
                    return Request(method, new Uri(respLoc), body, contentType, accept, headers);
                var response = new Response
                {
                    StatusCode = (ushort) webResponse.StatusCode,
                    StatusDescription = webResponse.StatusDescription,
                    ContentLength = (int) webResponse.ContentLength,
                    ContentType = accept ?? MimeTypes.JSON,
                    StreamedBody = webResponse.GetResponseStream()
                                   ?? throw new NullReferenceException("ResponseStream was null")
                };
                foreach (var header in webResponse.Headers.AllKeys)
                    response.Headers[header] = webResponse.Headers[header];
                return response;
            }
            catch (WebException we)
            {
                Log.Warn($"!!! HTTP {method} Error at {uri} : {we.Message}");
                if (!(we.Response is HttpWebResponse response)) return null;
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
                Log.Warn($"!!! HTTP {method} Error at {uri} : {e.Message}");
                return null;
            }
        }
    }
}