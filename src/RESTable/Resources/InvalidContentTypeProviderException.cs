namespace RESTable.Resources;

/// <inheritdoc />
/// <summary>
///     Thrown when RESTable encounters an error with an external protocol provider
/// </summary>
public class InvalidContentTypeProviderException : RESTableException
{
    internal InvalidContentTypeProviderException(string message) : base(ErrorCodes.InvalidContentTypeProvider,
        "An error was found in an external IContentTypeProvider: " + message) { }
}