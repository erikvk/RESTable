using System;
using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest.Aborted
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error profiling a given resource.
    /// </summary>
    internal class AbortedProfilerException<T> : AbortedOperation<T> where T : class
    {
        internal AbortedProfilerException(Exception ie, IRequest<T> request, string message = null)
            : base(ErrorCodes.AbortedCount, ie, request, message) { }
    }
}