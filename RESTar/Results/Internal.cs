using System;
using System.Net;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Internal errors used in RESTar
    /// </summary>
    public abstract class Internal : Error
    {
        internal Internal(ErrorCodes code, string message) : base(code, message) { }

        internal Internal(ErrorCodes code, string message, Exception ie) : base(code, message, ie)
        {
            StatusCode = HttpStatusCode.InternalServerError;
            StatusDescription = "Internal server error";
        }
    }
}