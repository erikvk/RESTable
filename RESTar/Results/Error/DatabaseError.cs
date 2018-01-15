using System;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class DatabaseError : Internal
    {
        public DatabaseError(Exception e) : base(ErrorCodes.DatabaseError, e.Message, e)
        {
            Headers["RESTar-info"] = "The Starcounter database encountered an error: " + (e.InnerException?.Message ?? e.Message);
        }
    }
}