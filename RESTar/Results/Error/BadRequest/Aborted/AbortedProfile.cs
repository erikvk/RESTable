using System;
using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest.Aborted
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error profiling a given resource.
    /// </summary>
    public class AbortedProfile<T> : AbortedOperation<T> where T : class
    {
        internal AbortedProfile(Exception ie, IQuery<T> query, string message = null)
            : base(ErrorCodes.AbortedCount, ie, query, message) { }
    }
}