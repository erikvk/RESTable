using System;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error profiling a given resource.
    /// </summary>
    public class AbortedProfile<T> : AbortedOperation<T> where T : class
    {
        internal AbortedProfile(IRequest<T> request, Exception ie, string message = null)
            : base(request, ErrorCodes.AbortedCount, ie, message) { }
    }
}