using System;
using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest.Aborted
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error deleting entities from a given resource.
    /// </summary>
    internal class AbortedDeleter<T> : AbortedOperation<T> where T : class
    {
        internal AbortedDeleter(Exception ie, IRequest<T> request, string message = null)
            : base(ErrorCodes.AbortedDelete, ie, request, message) { }
    }
}