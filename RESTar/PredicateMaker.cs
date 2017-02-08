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
                {
                    return (dict =>
                    {
                        dynamic val1 = dict.FirstOrDefault(pair => KeysEqual(pair.Key, c.Key)).Value;
                        dynamic val2 = c.Value;
                        try
                        {
                            return val1 == val2;
                        }
                        catch
                        {
                            return false;
                        }
                    });
                }
                if (c.Operator.Common == "!=")
                    return (dict =>
                    {
                        dynamic val1 = dict.FirstOrDefault(pair => KeysEqual(pair.Key, c.Key)).Value;
                        dynamic val2 = c.Value;
                        try
                        {
                            return !val1 == val2;
                        }
                        catch
                        {
                            return false;
                        }
                    });
                if (c.Operator.Common == "<")
                    return (dict =>
                    {
                        dynamic val1 = dict.FirstOrDefault(pair => KeysEqual(pair.Key, c.Key)).Value;
                        dynamic val2 = c.Value;
                        try
                        {
                            return val1 < val2;
                        }
                        catch
                        {
                            return false;
                        }
                    });
                if (c.Operator.Common == ">")
                    return (dict =>
                    {
                        dynamic val1 = dict.FirstOrDefault(pair => KeysEqual(pair.Key, c.Key)).Value;
                        dynamic val2 = c.Value;
                        try
                        {
                            return val1 > val2;
                        }
                        catch
                        {
                            return false;
                        }
                    });
                if (c.Operator.Common == ">=")
                    return (dict =>
                    {
                        dynamic val1 = dict.FirstOrDefault(pair => KeysEqual(pair.Key, c.Key)).Value;
                        dynamic val2 = c.Value;
                        try
                        {
                            return val1 >= val2;
                        }
                        catch
                        {
                            return false;
                        }
                    });
                if (c.Operator.Common == "<=")
                    return (dict =>
                    {
                        dynamic val1 = dict.FirstOrDefault(pair => KeysEqual(pair.Key, c.Key)).Value;
                        dynamic val2 = c.Value;
                        try
                        {
                            return val1 <= val2;
                        }
                        catch
                        {
                            return false;
                        }
                    });
                return default(Predicate<DDictionary>);
            }).ToList();
            return dictionary => innerPredicates.All(p => p != null && p(dictionary));
        }
    }
}