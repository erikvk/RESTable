using System;
using RESTable.Requests;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Thrown when RESTable aborts an operation due to some encountered error
/// </summary>
internal sealed class AbortedOperation : BadRequest
{
    internal AbortedOperation(IRequest request, ErrorCodes code, Exception ie) : base(code, ie.Message, ie)
    {
        Headers.Info = $"Aborted {request.Method} on resource '{request.Resource}' due to an error: {ie.Message}";
    }
}