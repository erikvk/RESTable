using System;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error inserting entities into a given resource.
    /// </summary>
    public class AbortedInserterException<T> : AbortedOperation<T> where T : class
    {
        internal AbortedInserterException(Exception ie, IRequest<T> request, string message = null)
            : base(ErrorCodes.AbortedInsert, ie, request, message) { }
    }
}