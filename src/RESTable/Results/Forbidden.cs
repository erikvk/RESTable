using System.Net;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable encounters a forbidden operation
    /// search string.
    /// </summary>
    public abstract class Forbidden : Error
    {
        internal Forbidden(ErrorCodes code, string info) : base(code, info)
        {
            StatusCode = HttpStatusCode.Forbidden;
            StatusDescription = "Forbidden";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(Forbidden)};{RequestInternal?.Resource};{ErrorCode}";
    }
}