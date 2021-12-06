using RESTable.Meta;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Thrown when an invalid value type was used in a condition or there was a mismatch
///     with the type of the referenced property.
/// </summary>
internal class InvalidConditionValueType : BadRequest
{
    internal InvalidConditionValueType(string valueLiteral, Member property)
        : base(ErrorCodes.InvalidConditionValueType,
            $"Invalid condition targeting member '{property.Name}' of type '{property.Owner?.GetRESTableTypeName() ?? "uknown"}': Condition value '{valueLiteral}' " +
            $"could not be converted to {property.Type.GetFriendlyTypeName()}") { }
}