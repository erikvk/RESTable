using System;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error counting entities in a given resource.
    /// </summary>
    public class AbortedReport<T> : AbortedOperation<T> where T : class
    {
        internal AbortedReport(IRequest<T> request, Exception ie, string message = null)
            : base(request, ErrorCodes.AbortedCount, ie, message) { }
    }
}