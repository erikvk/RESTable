using RESTar.Deflection.Dynamic;
using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    public class InvalidConditionValueType : BadRequest
    {
        internal InvalidConditionValueType(string valueLiteral, DeclaredProperty property)
            : base(ErrorCodes.InvalidConditionValueType, $"Invalid type for condition value '{valueLiteral}'. Expected {property.Type.Name}") { }
    }
}