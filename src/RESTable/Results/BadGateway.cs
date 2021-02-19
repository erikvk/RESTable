namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable encounters a bad gateway when connecting to a remote RESTable service
    /// </summary>
    public class BadGateway : Internal
    {
        public BadGateway(string uri) : base(ErrorCodes.ExternalServiceNotRESTable, "Encountered a bad gateway when connecting to " + uri) { }
    }
}