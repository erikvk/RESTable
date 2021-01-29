namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable encounters an unauthorized access attempt
    /// search string.
    /// </summary>
    internal class NotAuthorized : Forbidden
    {
        internal NotAuthorized() : base(ErrorCodes.NotAuthorized, "Not authorized") { }
    }
}