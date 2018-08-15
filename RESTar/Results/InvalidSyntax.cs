namespace RESTar.Results
{
    /// <inheritdoc />
    internal class InvalidSyntax : BadRequest
    {
        /// <inheritdoc />
        public InvalidSyntax(ErrorCodes errorCode, string message) : base(errorCode, "Syntax error: " + message) { }
    }
}