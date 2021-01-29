namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable receives a request with an unknown protocol indicator
    /// </summary>
    internal class UnknownProtocol : NotFound
    {
        internal UnknownProtocol(string searchString) : base(ErrorCodes.UnknownProtocol, $"Could not identify any protocol by '{searchString}'") { }
    }
}