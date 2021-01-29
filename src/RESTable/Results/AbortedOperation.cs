using System;
using Newtonsoft.Json;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable aborts an operation due to some encountered error
    /// </summary>
    internal sealed class AbortedOperation : BadRequest
    {
        internal AbortedOperation(IRequest request, ErrorCodes code, Exception ie, string message = null) : base(code,
            message ?? (ie is JsonSerializationException || ie is JsonReaderException ? "JSON serialization error, check JSON syntax." : ""), ie)
        {
            Headers.Info = $"Aborted {request.Method} on resource '{request.Resource}' due to an error: {this.TotalMessage()}";
        }
    }
}