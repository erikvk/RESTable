namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable cannot get entities from an external source
    /// </summary>
    internal class InvalidExternalSource : BadRequest
    {
        internal InvalidExternalSource(string uri, string message) : base(ErrorCodes.InvalidSourceData,
            $"RESTable could not get entities from source at '{uri}'. {message}") { }
    }
}