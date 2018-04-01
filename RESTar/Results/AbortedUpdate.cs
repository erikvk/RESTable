using System;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error updating entities in a given resource.
    /// </summary>
    internal class AbortedUpdate<T> : AbortedOperation<T> where T : class
    {
        internal AbortedUpdate(IRequest<T> request, Exception ie, string message = null)
            : base(request, ErrorCodes.AbortedUpdate, ie, message) { }
    }
}