using System;
using System.Net;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Internal errors used in RESTable
    /// </summary>
    public abstract class Internal : Error
    {
        protected internal Internal(ErrorCodes code, string info, Exception ie = null) : base(code, info, ie)
        {
            StatusCode = HttpStatusCode.InternalServerError;
            StatusDescription = "Internal server error";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(Internal)};{Request?.Resource};{ErrorCode}";
    }
}