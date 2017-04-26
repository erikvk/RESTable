using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using static System.StringComparison;

namespace RESTar
{
    internal static class PredicateMaker
    {
        private static bool KeysEqual(string k1, string k2) =>
            string.Equals(k1, k2, CurrentCultureIgnoreCase);

        internal static Predicate<DDictionary> ToDDictionaryPredicate(this IEnumerable<Condition> conditions)
        {
            var innerPredicates = conditions.Select(c =>
                {
                    switch (c.Operator.OpCode)
                    {
                        case Operators.EQUALS:
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
                        case Operators.NOT_EQUALS:
                            return (dict =>
                            {
                                dynamic val1 = dict.FirstOrDefault(pair => KeysEqual(pair.Key, c.Key)).Value;
                                dynamic val2 = c.Value;
                                try
                                {
                                    return val1 != val2;
                                }
                                catch
                                {
                                    return false;
                                }
                            });
                        case Operators.LESS_THAN:
                            return (dict =>
                            {
                                dynamic val1 = dict.FirstOrDefault(pair => KeysEqual(pair.Key, c.Key)).Value;
                                dynamic val2 = c.Value;
                                try
                                {
                                    if (val1 is string && val2 is string)
                                        return string.Compare((string) val1, (string) val2, Ordinal) < 0;
                                    return val1 < val2;
                                }
                                catch
                                {
                                    return false;
                                }
                            });
                        case Operators.GREATER_THAN:
                            return (dict =>
                            {
                                dynamic val1 = dict.FirstOrDefault(pair => KeysEqual(pair.Key, c.Key)).Value;
                                dynamic val2 = c.Value;
                                try
                                {
                                    if (val1 is string && val2 is string)
                                        return string.Compare((string) val1, (string) val2, Ordinal) > 0;
                                    return val1 > val2;
                                }
                                catch
                                {
                                    return false;
                                }
                            });
                        case Operators.GREATER_THAN_OR_EQUALS:
                            return (dict =>
                            {
                                dynamic val1 = dict.FirstOrDefault(pair => KeysEqual(pair.Key, c.Key)).Value;
                                dynamic val2 = c.Value;
                                try
                                {
                                    if (val1 is string && val2 is string)
                                        return string.Compare((string) val1, (string) val2, Ordinal) >= 0;
                                    return val1 >= val2;
                                }
                                catch
                                {
                                    return false;
                                }
                            });
                        case Operators.LESS_THAN_OR_EQUALS:
                            return (dict =>
                            {
                                dynamic val1 = dict.FirstOrDefault(pair => KeysEqual(pair.Key, c.Key)).Value;
                                dynamic val2 = c.Value;
                                try
                                {
                                    if (val1 is string && val2 is string)
                                        return string.Compare((string) val1, (string) val2, Ordinal) <= 0;
                                    return val1 <= val2;
                                }
                                catch
                                {
                                    return false;
                                }
                            });
                    }
                    return default(Predicate<DDictionary>);
                })
                .ToList();
            return dictionary => innerPredicates.All(p => p != null && p(dictionary));
        }

        internal static ICollection<T> EvaluateEntitites<T>(this IRequest request, IEnumerable<T> entities)
            where T : class
        {
            var type = typeof(T);
            var dynamicEvaluation = type != request.Resource.TargetType;
            if (dynamicEvaluation)
            {
                request.Conditions?.Migrate(type);
                request.MetaConditions.OrderBy?.Migrate(type);
            }
            if (request.MetaConditions.OrderBy != null)
                entities = request.MetaConditions.OrderBy.Ascending
                    ? entities.OrderBy(request.MetaConditions.OrderBy.ToSelector<T>())
                    : entities.OrderByDescending(request.MetaConditions.OrderBy.ToSelector<T>());
            if (request.Conditions != null)
                entities = request.Conditions.Evaluate(entities);
            if (request.MetaConditions.Limit < 1)
                return entities.ToList();
            return entities.Take(request.MetaConditions.Limit).ToList();
        }

//        internal static Predicate<T> ToPredicate<T>(this IEnumerable<Condition> conditions)
//        {
//            var type = typeof(T);
//            if (type == typeof(DDictionary) || type.IsSubclassOf(typeof(DDictionary)))
//            {
//                throw new ArgumentException("ToPredicate() cannot be called on DDictionary types. Use " +
//                                            "ToDDictionaryPredicate() instead");
//            }
//
//            var innerPredicates = conditions.Select(c =>
//                {
//                    switch (c.Operator.OpCode)
//                    {
//                        case Operators.EQUALS:
//                            return item =>
//                            {
//                                try
//                                {
//                                    dynamic value;
//                                    ExtensionMethods.GetValueFromKeyString(type, c.Key, item, out value);
//                                    return value == c.Value;
//                                }
//                                catch
//                                {
//                                    return false;
//                                }
//                            };
//                        case Operators.NOT_EQUALS:
//                            return item =>
//                            {
//                                try
//                                {
//                                    dynamic value;
//                                    ExtensionMethods.GetValueFromKeyString(type, c.Key, item, out value);
//                                    return value != c.Value;
//                                }
//                                catch
//                                {
//                                    return true;
//                                }
//                            };
//                        case Operators.LESS_THAN:
//                            return item =>
//                            {
//                                try
//                                {
//                                    dynamic value;
//                                    ExtensionMethods.GetValueFromKeyString(type, c.Key, item, out value);
//                                    if (value is string && c.Value is string)
//                                        return string.Compare((string) value, (string) c.Value, Ordinal) < 0;
//                                    return value < c.Value;
//                                }
//                                catch
//                                {
//                                    return false;
//                                }
//                            };
//                        case Operators.GREATER_THAN:
//                            return item =>
//                            {
//                                try
//                                {
//                                    dynamic value;
//                                    ExtensionMethods.GetValueFromKeyString(type, c.Key, item, out value);
//                                    if (value is string && c.Value is string)
//                                        return string.Compare((string) value, (string) c.Value, Ordinal) > 0;
//                                    return value > c.Value;
//                                }
//                                catch
//                                {
//                                    return false;
//                                }
//                            };
//                        case Operators.GREATER_THAN_OR_EQUALS:
//                            return item =>
//                            {
//                                try
//                                {
//                                    dynamic value;
//                                    ExtensionMethods.GetValueFromKeyString(type, c.Key, item, out value);
//                                    if (value is string && c.Value is string)
//                                        return string.Compare((string) value, (string) c.Value, Ordinal) >= 0;
//                                    return value >= c.Value;
//                                }
//                                catch
//                                {
//                                    return false;
//                                }
//                            };
//                        case Operators.LESS_THAN_OR_EQUALS:
//                            return item =>
//                            {
//                                try
//                                {
//                                    dynamic value;
//                                    ExtensionMethods.GetValueFromKeyString(type, c.Key, item, out value);
//                                    if (value is string && c.Value is string)
//                                        return string.Compare((string) value, (string) c.Value, Ordinal) <= 0;
//                                    return value <= c.Value;
//                                }
//                                catch
//                                {
//                                    return false;
//                                }
//                            };
//                    }
//
//                    return default(Predicate<T>);
//                })
//                .ToList();
//
//            return item => innerPredicates.All(p => p != null && p(item));
//        }
    }
}