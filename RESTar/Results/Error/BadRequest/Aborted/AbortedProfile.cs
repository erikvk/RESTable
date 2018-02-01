using System;
using RESTar.Internal;

namespace RESTar.Results.Fail.BadRequest.Aborted
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error profiling a given resource.
    /// </summary>
    internal class AbortedProfile<T> : AbortedOperation<T> where T : class
    {
        internal AbortedProfile(Exception ie, IRequest<T> request, string message = null)
            : base(ErrorCodes.AbortedCount, ie, request, message) { }
    }
}