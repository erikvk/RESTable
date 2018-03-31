using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters a request evaluation that cannot be traced
    /// </summary>
    public class ReusedContext : Error
    {
        internal ReusedContext() : base(ErrorCodes.ReusedContext,
            "An attempt was made to reuse an existing context for a new request evaluation. " +
            "Each request needs a unique context.") { }
    }
}