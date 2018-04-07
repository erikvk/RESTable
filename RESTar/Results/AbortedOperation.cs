using System;
using Newtonsoft.Json;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar aborts an operation due to some encountered error
    /// </summary>
    public abstract class AbortedOperation<T> : BadRequest where T : class
    {
        internal AbortedOperation(IRequest<T> request, ErrorCodes code, Exception ie, string message = null) : base(code,
            message ?? (ie is JsonSerializationException || ie is JsonReaderException ? "JSON serialization error, check JSON syntax." : ""), ie)
        {
            Headers["RESTar-info"] = $"Aborted {request.Method} on resource '{request.Resource}' due to an error: {this.TotalMessage()}";
        }
    }
}