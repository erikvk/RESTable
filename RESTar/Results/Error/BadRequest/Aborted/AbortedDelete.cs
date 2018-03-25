using System;
using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest.Aborted
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error deleting entities from a given resource.
    /// </summary>
    public class AbortedDelete<T> : AbortedOperation<T> where T : class
    {
        internal AbortedDelete(Exception ie, IQuery<T> query, string message = null)
            : base(ErrorCodes.AbortedDelete, ie, query, message) { }
    }
}