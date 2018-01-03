using System;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error counting entities in a given resource.
    /// </summary>
    public class AbortedCounterException<T> : AbortedOperation<T> where T : class
    {
        internal AbortedCounterException(Exception ie, IRequest<T> request, string message = null)
            : base(ErrorCodes.AbortedCount, ie, request, message) { }
    }
}