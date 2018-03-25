using System;
using Newtonsoft.Json;
using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest.Aborted
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar aborts an operation due to some encountered error
    /// </summary>
    public abstract class AbortedOperation<T> : BadRequest where T : class
    {
        internal AbortedOperation(ErrorCodes code, Exception ie, IQuery<T> query, string message = null) : base(code,
            message ?? (ie is JsonSerializationException || ie is JsonReaderException ? "JSON serialization error, check JSON syntax." : ""), ie)
        {
            Headers["RESTar-info"] = $"Aborted {query.Method} on resource '{query.Resource}' due to an error: {this.TotalMessage()}";
        }
    }
}