using System;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    public class DatabaseError : Internal
    {
        internal DatabaseError(Exception e) : base(ErrorCodes.DatabaseError, e.Message, e)
        {
            Headers["RESTar-info"] = "The Starcounter database encountered an error: " + (e.InnerException?.Message ?? e.Message);
        }
    }
}