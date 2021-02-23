using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Linq;

namespace RESTable.Internal
{
    internal class HttpRequest : ITraceable
    {
        public Method Method { get; }
        public string URI { get; }
        public Headers Headers { get; }
        public Func<Stream, Task> WriteBody { get; }
        public RESTableContext Context { get; }
        public async Task<HttpResponse> GetResponseAsync() => await MakeExternalRequestAsync(this, Method.ToString(), new Uri(URI), WriteBody, Headers).ConfigureAwait(false);

        internal HttpRequest(ITraceable trace, HeaderRequestParameters parameters, Func<Stream, Task> writeBody)
        {
            Context = trace.Context;
            URI = parameters.URI;
            WriteBody = writeBody;
            Headers = parameters.Headers;
            Method = parameters.Method;
        }

        private static async Task<HttpResponse> MakeExternalRequestAsync(ITraceable trace, string method, Uri uri, Func<Stream, Task> writeBody, IHeaders headers)
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
                if (writeBody != null)
                {
                    await using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
                    {
                        await writeBody(requestStream).ConfigureAwait(false);
                    }
                }
                var webResponse = (HttpWebResponse) await request.GetResponseAsync().ConfigureAwait(false);
                var respLoc = webResponse.Headers["Location"];
                if (webResponse.StatusCode == HttpStatusCode.MovedPermanently && respLoc != null)
                    return await MakeExternalRequestAsync(trace, method, new Uri(respLoc), writeBody, headers).ConfigureAwait(false);
                return new HttpResponse(trace, webResponse);
            }
            catch (WebException we)
            {
                if (!(we.Response is HttpWebResponse response)) return null;
                var _response = new HttpResponse(trace, response.StatusCode, response.StatusDescription);
                foreach (var header in response.Headers.AllKeys)
                    _response.Headers[header] = response.Headers[header];
                return _response;
            }
            catch (Exception e)
            {
                return new HttpResponse(trace, HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }
}