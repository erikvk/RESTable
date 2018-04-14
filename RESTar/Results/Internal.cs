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
        internal Internal(ErrorCodes code, string info, Exception ie) : base(code, info, ie)
        {
            StatusCode = HttpStatusCode.InternalServerError;
            StatusDescription = "Internal server error";
        }
    }
}