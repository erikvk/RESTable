using System;
using RESTar.Internal;

namespace RESTar
{
    public abstract class RESTarException : Exception
    {
        /// <summary>
        /// The error code for this error
        /// </summary>
        public ErrorCodes ErrorCode { get; }

        protected RESTarException(ErrorCodes errorCode, string message, Exception ie = null) : base(message, ie)
        {
            ErrorCode = errorCode;
        }
    }
}