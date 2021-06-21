using System;
using System.Text.RegularExpressions;
using RESTable.Internal;
using RESTable.Results;

namespace RESTable.Requests
{
    internal class HeaderRequestParameters : IProtocolHolder
    {
        /// <summary>
        /// The request that contained the header that encoded these request parameters
        /// </summary>
        private IRequest Request { get; }

        /// <summary>
        /// The name of the header that encoded these request parameters
        /// </summary>
        public string HeaderName { get; }

        /// <summary>
        /// The method of the request paramters
        /// </summary>
        public Method Method { get; }

        /// <summary>
        /// The URI of the request paramters
        /// </summary>
        public string Uri { get; }

        /// <summary>
        /// The headers of the request paramters
        /// </summary>
        public Headers Headers { get; }

        /// <summary>
        /// Are these request parameters for an internal request?
        /// </summary>
        public bool IsInternal { get; }

        public RESTableContext Context => Request.Context;

        public string? HeadersStringCache { get; set; }

        public bool ExcludeHeaders => false;

        public string ProtocolIdentifier => null;

        public CachedProtocolProvider CachedProtocolProvider => throw new NotImplementedException();

        public static bool TryParse(IRequest request, string headerName, string headerValue, out HeaderRequestParameters parameters, out Results.Error error)
        {
            try
            {
                parameters = new HeaderRequestParameters(request, headerName, headerValue);
                error = null;
                return true;
            }
            catch (Error _error)
            {
                parameters = null;
                error = _error;
                return false;
            }
            catch (Exception exception)
            {
                parameters = null;
                error = exception.AsError();
                return false;
            }
        }

        public HeaderRequestParameters(IRequest request, string headerName, string headerValue)
        {
            Request = request;
            var matches = Regex.Match(headerValue, RegEx.HeaderRequestParameters);
            if (!matches.Success)
                throw new HttpRequestException($"Invalid request syntax in {headerName} header");
            var methodString = matches.Groups["method"].Value;
            if (!Enum.TryParse(methodString, true, out Method method))
                throw new HttpRequestException($"Invalid or missing method in {headerName} header");
            Method = method;
            Uri = matches.Groups["uri"].Value;
            IsInternal = Uri[0] == '/';
            Headers = new Headers();
            var headersGroup = matches.Groups["headers"];
            if (!headersGroup.Success) return;
            foreach (Match match in Regex.Matches(headersGroup.Value, RegEx.HeaderRequestParametersRequestHeader))
            {
                var header = match.Groups["header"].Value.Trim();
                var value = match.Groups["value"].Value.Trim();
                Headers[header] = value;
            }
        }
    }
}