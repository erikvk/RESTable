using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;

namespace RESTar.Results.Fail.BadRequest
{
    internal class BadConditionOperator : BadRequest
    {
        internal BadConditionOperator(TerminalResource terminal, Operator found) : base(ErrorCodes.InvalidConditionOperator,
            $"Invalid operator '{found.Common}' in condition to terminal resource '{terminal.Name}'. Only \'=\' is valid in terminal conditions.") { }

        internal BadConditionOperator(string c, ITarget target, Operator found, Term term, IEnumerable<Operator> allowed)
            : base(ErrorCodes.InvalidConditionOperator, $"Forbidden operator for condition '{c}'. '{found}' is not allowed when " +
                                                        $"comparing against '{term.Key}' in type '{target.Name}'. Allowed operators" +
                                                        $": {string.Join(", ", allowed.Select(a => $"'{a.Common}'"))}") { }
    }
}