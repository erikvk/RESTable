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
        internal AbortedInsert(IRequest<T> request, Exception ie, string message = null)
            : base(request, ErrorCodes.AbortedInsert, ie, message) { }
    }
}