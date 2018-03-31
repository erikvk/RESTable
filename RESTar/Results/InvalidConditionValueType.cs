using RESTar.Internal;
using RESTar.Reflection.Dynamic;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an invalid value type was used in a condition or there was a mismatch 
    /// with the type of the referenced property.
    /// </summary>
    public class InvalidConditionValueType : BadRequest
    {
        internal InvalidConditionValueType(string valueLiteral, DeclaredProperty property)
            : base(ErrorCodes.InvalidConditionValueType, $"Invalid type for condition value '{valueLiteral}'. Expected {property.Type.Name}") { }
    }
}