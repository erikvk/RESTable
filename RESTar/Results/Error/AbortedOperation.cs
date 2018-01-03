using System;
using Newtonsoft.Json;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <summary>
    /// A common class for instances when an operation was aborted
    /// </summary>
    public class AbortedOperation<T> : BadRequest where T : class
    {
        internal AbortedOperation(ErrorCodes code, Exception ie, IRequest<T> request, string message = null) : base(code,
            message ?? (ie is JsonSerializationException || ie is JsonReaderException ? "JSON serialization error, check JSON syntax. " : ""))
        {
            Headers["RESTar-info"] = $"Aborted {request.Method} on resource '{request.Resource}' " +
                                     $"due to an error: {this.TotalMessage()}";
        }
    }
}