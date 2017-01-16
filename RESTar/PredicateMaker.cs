using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;

namespace RESTar
{
    internal static class PredicateMaker
    {
        private static bool KeysEqual(string k1, string k2) =>
            string.Equals(k1, k2, StringComparison.CurrentCultureIgnoreCase);

        internal static Predicate<DDictionary> DDictPredicate(this IEnumerable<Condition> conditions)
        {
            var innerPredicates = conditions.Select(c =>
            {
                if (c.Operator.Common == "=")
                    return (dict => dict.FirstOrDefault(pair => KeysEqual(pair.Key, c.Key)).Value.Equals(c.Value));
                if (c.Operator.Common == "!=")
                    return (dict => !dict.FirstOrDefault(pair => KeysEqual(pair.Key, c.Key)).Value.Equals(c.Value));
                if (c.Operator.Common == "<")
                    return (dict => (dynamic) dict.FirstOrDefault(pair => KeysEqual(pair.Key, c.Key)).Value < c.Value);
                if (c.Operator.Common == ">")
                    return (dict => (dynamic) dict.FirstOrDefault(pair => KeysEqual(pair.Key, c.Key)).Value > c.Value);
                if (c.Operator.Common == ">=")
                    return (dict => (dynamic) dict.FirstOrDefault(pair => KeysEqual(pair.Key, c.Key)).Value >= c.Value);
                if (c.Operator.Common == "<=")
                    return (dict => (dynamic) dict.FirstOrDefault(pair => KeysEqual(pair.Key, c.Key)).Value <= c.Value);
                return default(Predicate<DDictionary>);
            }).ToList();
            return dictionary => innerPredicates.All(p => p != null && p(dictionary));
        }
    }
}