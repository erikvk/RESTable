using System;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable encounters an unkown error
    /// </summary>
    internal class Unknown : Internal
    {
        /// <inheritdoc />
        public Unknown(Exception e) : base(ErrorCodes.Unknown, e.Message, e) { }
    }
}