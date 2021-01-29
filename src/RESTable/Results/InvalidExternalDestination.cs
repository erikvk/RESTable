using RESTable.Internal;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable cannot upload entities to an external destination
    /// </summary>
    internal class InvalidExternalDestination : BadRequest
    {
        internal InvalidExternalDestination(HttpRequest request, string message) : base(ErrorCodes.InvalidDestination,
            $"RESTable could not upload entities to destination at '{request.URI}': {message}") { }
    }
}