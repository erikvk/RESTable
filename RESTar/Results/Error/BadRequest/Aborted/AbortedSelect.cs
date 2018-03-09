using System;
using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest.Aborted
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error selecting entities from a given resource.
    /// </summary>
    public class AbortedSelect<T> : AbortedOperation<T> where T : class
    {
        internal AbortedSelect(Exception ie, IRequest<T> request, string message = null)
            : base(ErrorCodes.AbortedSelect, ie, request, message) { }
    }
}