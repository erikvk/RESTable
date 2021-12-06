using System.Net;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Thrown when RESTable encounters an infinite loop of recursive internal requests
/// </summary>
public class InfiniteLoop : Error
{
    public InfiniteLoop() : base(ErrorCodes.InfiniteLoopDetected,
        "RESTable encountered a potentially infinite loop of recursive internal calls.")
    {
        StatusCode = (HttpStatusCode) 508;
        StatusDescription = "Infinite loop detected";
    }

    /// <inheritdoc />
    public override string Metadata => $"{nameof(InfiniteLoop)};{Request.Resource};{ErrorCode}";
}