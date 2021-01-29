namespace RESTable.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable encounters an error with an external protocol provider
    /// </summary>
    public class InvalidProtocolProviderException : RESTableException
    {
        internal InvalidProtocolProviderException(string message) : base(ErrorCodes.InvalidProtocolProvider,
            "An error was found in an external IProtocolProvider: " + message) { }
    }
}