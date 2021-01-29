namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable is unable to get a response from a remote service request
    /// </summary>
    public class Timeout : NotFound
    {
        internal Timeout(string uri) : base(ErrorCodes.NoResponseFromRemoteService,
            "No response from remote service at " + uri) { }
    }
}