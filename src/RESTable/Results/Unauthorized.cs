using System.Net;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Thrown when RESTable encounters an unauthorized access attempt
///     search string.
/// </summary>
public class Unauthorized : Error
{
    public Unauthorized() : base(ErrorCodes.NotAuthorized, "Not authorized")
    {
        StatusCode = HttpStatusCode.Unauthorized;
        StatusDescription = "Unauthorized";
        Headers["WWW-Authenticate"] = "Basic realm=\"REST API\", charset=\"UTF-8\"";
    }

    public override string Metadata => $"{nameof(Unauthorized)};{Request.Resource};{ErrorCode}";
}
