using System;
using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest.Aborted
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error inserting entities into a given resource.
    /// </summary>
    public class AbortedInsert<T> : AbortedOperation<T> where T : class
    {
        internal AbortedInsert(Exception ie, IQuery<T> query, string message = null)
            : base(ErrorCodes.AbortedInsert, ie, query, message) { }
    }
}