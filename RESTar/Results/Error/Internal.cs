using System;
using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Internal errors used in RESTar
    /// </summary>
    public abstract class Internal : RESTarError
    {
        internal Internal(ErrorCodes code, string message, Exception ie) : base(code, message, ie)
        {
            StatusCode = HttpStatusCode.InternalServerError;
            StatusDescription = "Internal server error";
        }
    }
}