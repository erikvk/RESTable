using System;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error inserting entities into a given resource.
    /// </summary>
    public class AbortedInsert<T> : AbortedOperation<T> where T : class
    {
        internal AbortedInsert(Exception ie, IRequest<T> request, string message = null)
            : base(ErrorCodes.AbortedInsert, ie, request, message) { }
    }
}