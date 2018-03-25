using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an invalid number of entities was contained in a request body
    /// </summary>
    public class InvalidInputCount : BadRequest
    {
        internal InvalidInputCount(int expecterNumberOfEntities) : base(ErrorCodes.DataSourceFormat,
            $"Invalid entity count. Expected {expecterNumberOfEntities} entities for this operation, " +
            "but found additional.") { }
    }
}