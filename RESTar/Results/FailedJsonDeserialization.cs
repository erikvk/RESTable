using System;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error when reading JSON
    /// </summary>
    public class FailedJsonDeserialization : BadRequest
    {
        internal FailedJsonDeserialization(Exception ie) : base(ErrorCodes.FailedJsonDeserialization, null, ie)
        {
            Headers["RESTar-info"] = "Error while deserializing JSON. Check JSON syntax.";
        }
    }
}