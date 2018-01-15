using System;

namespace RESTar.Results.Error
{
    internal class HttpRequestException : Exception
    {
        public HttpRequestException(string message) : base(message) { }
    }
}