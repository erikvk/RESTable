using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an invalid number of data entities was provided for a certain method.
    /// </summary>
    public class InvalidInputCount : Base
    {
        internal InvalidInputCount() : base(ErrorCodes.DataSourceFormat,
            "Invalid input count. Expected object/row, but found array/multiple rows. " +
            "Only POST accepts multiple objects/rows as input.") { }
    }
}