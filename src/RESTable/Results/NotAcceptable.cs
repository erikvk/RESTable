using System.Net;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Thrown when RESTable encounters an invalid or unsupported content type in the Accept header of a request.
/// </summary>
public class NotAcceptable : Error
{
    /// <inheritdoc />
    public NotAcceptable(string headerValue, string? expected = null) : base
    (
        ErrorCodes.NotAcceptable,
        $"No supported media types were found in the Accept header. Found '{headerValue}'{(expected is not null ? $", expected {expected}" : null)}"
    )
    {
        StatusCode = HttpStatusCode.NotAcceptable;
        StatusDescription = "Not acceptable";
    }

    /// <inheritdoc />
    public override string Metadata => $"{nameof(NotAcceptable)};{Request.Resource};{ErrorCode}";
}
