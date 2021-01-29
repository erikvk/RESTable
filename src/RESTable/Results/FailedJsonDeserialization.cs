using Newtonsoft.Json;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable encounters an error when reading JSON
    /// </summary>
    internal class FailedJsonDeserialization : BadRequest
    {
        internal FailedJsonDeserialization(JsonReaderException ie) : base(ErrorCodes.FailedJsonDeserialization,
            $"JSON syntax error: {ie.Message}", ie) { }
    }
}