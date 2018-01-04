using System;
using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest.Aborted
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error updating entities in a given resource.
    /// </summary>
    internal class AbortedUpdaterException<T> : AbortedOperation<T> where T : class
    {
        internal AbortedUpdaterException(Exception ie, IRequest<T> request, string message = null)
            : base(ErrorCodes.AbortedUpdate, ie, request, message) { }
    }
}