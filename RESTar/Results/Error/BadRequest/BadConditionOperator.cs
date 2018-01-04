using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a provided operator was forbidden for the given property
    /// </summary>
    public class BadConditionOperator : Base
    {
        internal BadConditionOperator(string c, ITarget target, Operator found, Term term, IEnumerable<Operator> allowed)
            : base(ErrorCodes.InvalidConditionOperator, $"Forbidden operator for condition '{c}'. '{found}' is not allowed when " +
                                                        $"comparing against '{term.Key}' in type '{target.Name}'. Allowed operators" +
                                                        $": {string.Join(", ", allowed.Select(a => $"'{a.Common}'"))}") { }
    }
}