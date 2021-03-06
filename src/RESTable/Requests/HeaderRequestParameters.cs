using System;
using System.Text.RegularExpressions;
using RESTable.Internal;

namespace RESTable.Requests
{
    internal class HeaderRequestParameters
    {
        public Method Method { get; }
        public string URI { get; }
        public Headers Headers { get; }
        public bool IsInternal { get; }

        public static bool TryParse(string destinationHeader, out HeaderRequestParameters parameters, out Results.Error error)
        {
            try
            {
                parameters = new HeaderRequestParameters(destinationHeader);
                error = null;
                return true;
            }
            catch (Results.Error _error)
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

        public HeaderRequestParameters(string headerValue)
        {
            var matches = Regex.Match(headerValue, RegEx.HeaderRequestParameters);
            if (!matches.Success)
                throw new HttpRequestException("Invalid request syntax");
            var methodString = matches.Groups["method"].Value;
            if (!Enum.TryParse(methodString, true, out Method method))
                throw new HttpRequestException("Invalid or missing method");
            Method = method;
            URI = matches.Groups["uri"].Value;
            IsInternal = URI[0] == '/';
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