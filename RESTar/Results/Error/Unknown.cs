using System;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an unkown error
    /// </summary>
    public class Unknown : Internal
    {
        /// <inheritdoc />
        public Unknown(Exception e) : base(ErrorCodes.Unknown, e.Message, e) { }
    }
}