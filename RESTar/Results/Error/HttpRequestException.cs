using System;

namespace RESTar.Results.Fail
{
    internal class HttpRequestException : Exception
    {
        public HttpRequestException(string message) : base(message) { }
    }
}