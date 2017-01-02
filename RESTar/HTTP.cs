using System;
using System.Collections.Generic;
using Starcounter;

namespace RESTar
{
    internal static class HTTP
    {
        internal static Response Request(string method, string uri, string bodyString = null, string contentType = null,
            string accept = null, Dictionary<string, string> headers = null, string log = null,
            Action<Response> then = null)
        {
            try
            {
                if (uri.Contains("https://"))
                    return HTTPS.Request(method, uri, bodyString, contentType, accept, headers, log, then);
                if (!uri.Contains("http"))
                    uri = "http://" + uri;

                return Http.CustomRESTRequest(method, uri, bodyString ?? "", headers);
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}