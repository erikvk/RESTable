using System;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error deleting entities from a given resource.
    /// </summary>
    public class AbortedDelete<T> : AbortedOperation<T> where T : class
    {
        internal AbortedDelete(Exception ie, IRequest<T> request, string message = null)
            : base(ErrorCodes.AbortedDelete, ie, request, message) { }
    }
}