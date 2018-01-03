using System;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error selecting entities from a given resource.
    /// </summary>
    public class AbortedSelectorException<T> : AbortedOperation<T> where T : class
    {
        internal AbortedSelectorException(Exception ie, IRequest<T> request, string message = null)
            : base(ErrorCodes.AbortedSelect, ie, request, message) { }
    }
}