using System;
using RESTar.Internal;

namespace RESTar.Starcounter
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error with the Starcounter database
    /// search string.
    /// </summary>
    public class StarcounterDatabaseError : Results.Error.Internal
    {
        internal StarcounterDatabaseError(Exception e) : base(ErrorCodes.DatabaseError, e.Message, e)
        {
            Headers["RESTar-info"] = "The Starcounter database encountered an error: " + (e.InnerException?.Message ?? e.Message);
        }
    }
}