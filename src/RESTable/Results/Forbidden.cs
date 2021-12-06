using System.Net;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Thrown when RESTable encounters a forbidden operation
///     search string.
/// </summary>
public abstract class Forbidden : Error
{
    public Forbidden(ErrorCodes code, string info) : base(code, info)
    {
        StatusCode = HttpStatusCode.Forbidden;
        StatusDescription = "Forbidden";
    }

    /// <inheritdoc />
    public override string Metadata => $"{nameof(Forbidden)};{Request.Resource};{ErrorCode}";
}