using System;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error selecting entities from a given resource.
    /// </summary>
    public class AbortedSelect<T> : AbortedOperation<T> where T : class
    {
        internal AbortedSelect(IRequest<T> request, Exception ie, string message = null)
            : base(request, ErrorCodes.AbortedSelect, ie, message) { }
    }
}