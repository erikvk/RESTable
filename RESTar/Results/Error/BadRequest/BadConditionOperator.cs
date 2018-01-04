using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;

namespace RESTar.Results.Fail.BadRequest
{
    internal class BadConditionOperator : BadRequest
    {
        internal BadConditionOperator(string c, ITarget target, Operator found, Term term, IEnumerable<Operator> allowed)
            : base(ErrorCodes.InvalidConditionOperator, $"Forbidden operator for condition '{c}'. '{found}' is not allowed when " +
                                                        $"comparing against '{term.Key}' in type '{target.FullName}'. Allowed operators" +
                                                        $": {string.Join(", ", allowed.Select(a => $"'{a.Common}'"))}") { }
    }
}