using System;
using RESTar.Internal;

namespace RESTar.Results.Fail.BadRequest
{
    internal class FailedJsonDeserialization : BadRequest
    {
        internal FailedJsonDeserialization(Exception ie) : base(ErrorCodes.FailedJsonDeserialization, ie)
        {
            Headers["RESTar-info"] = "Error while deserializing JSON. Check JSON syntax.";
        }
    }
}