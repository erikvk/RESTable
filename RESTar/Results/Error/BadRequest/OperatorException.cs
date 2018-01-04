using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a provided operator was invalid
    /// </summary>
    public class OperatorException : InvalidSyntax
    {
        internal OperatorException(string c) : base(ErrorCodes.InvalidConditionOperator,
            $"Invalid or missing operator or separator ('&') for condition '{c}'. Always URI encode all equals ('=' -> '%3D') " +
            "and exclamation marks ('!' -> '%21') in condition literals to avoid capture with reserved characters.") { }
    }
}