using System;

namespace RESTable
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable encounters a call to an operation that cannot be executed
    /// before RESTableConfig.Init() has been called.
    /// </summary>
    public class NotInitializedException : RESTableException
    {
        internal NotInitializedException(string message, Exception ie = null) : base(ErrorCodes.NotInitialized, message, ie) { }
    }
}