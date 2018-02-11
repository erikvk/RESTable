using System;

namespace RESTar.Results.Error
{
    public class HttpRequestException : Exception
    {
        public HttpRequestException(string message) : base(message) { }
    }
}