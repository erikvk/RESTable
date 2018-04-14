using System;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an HTTP request failed due to some error
    /// </summary>
    public class HttpRequestException : Exception
    {
        /// <inheritdoc />
        public HttpRequestException(string message) : base(message) { }
    }
}