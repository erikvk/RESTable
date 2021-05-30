using System;

namespace RESTable
{
    /// <inheritdoc />
    /// <summary>
    /// A common base class for all RESTable exceptions
    /// </summary>
    public abstract class RESTableException : Exception
    {
        /// <summary>
        /// The error code for this error
        /// </summary>
        public ErrorCodes ErrorCode { get; }

        /// <inheritdoc />
        protected RESTableException(ErrorCodes errorCode, string? message, Exception? ie = null) : base(message, ie)
        {
            ErrorCode = errorCode;
        }
    }
}