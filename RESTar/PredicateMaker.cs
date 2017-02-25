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

        internal static Predicate<DDictionary> ToDDictionaryPredicate(this IEnumerable<Condition> conditions)
        {
            var innerPredicates = conditions.Select(c =>
            {
                switch (c.Operator.Common)
                {
                    case "=":
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
                    case "!=":
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
                    case "<":
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
                    case ">":
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
                    case ">=":
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
                    case "<=":
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
                }
                return default(Predicate<DDictionary>);
            }).ToList();
            return dictionary => innerPredicates.All(p => p != null && p(dictionary));
        }

        internal static ICollection<T> Evaluate<T>(this OrderBy orderBy, IEnumerable<T> entities)
        {
            if (orderBy.Ascending)
                return entities.OrderBy(orderBy.GetSelector<T>()).ToList();
            return entities.OrderByDescending(orderBy.GetSelector<T>()).ToList();
        }

        internal static Func<T1, dynamic> GetSelector<T1>(this OrderBy orderBy)
        {
            return item =>
            {
                try
                {
                    dynamic value;
                    var str = ExtensionMethods.GetValueFromKeyString(typeof(T1), orderBy.Key, item, out value);
                    return value;
                }
                catch
                {
                    return null;
                }
            };
        }

        internal static ICollection<T> EvaluateEntitites<T>(this IRequest request, IEnumerable<T> entities)
        {
            if (request.OrderBy != null)
                entities = request.OrderBy.Ascending
                    ? entities.OrderBy(request.OrderBy.GetSelector<T>())
                    : entities.OrderByDescending(request.OrderBy.GetSelector<T>());
            if (request.Conditions != null)
                entities = entities.Where(request.Conditions.ToPredicate<T>().Invoke);
            if (request.Limit < 1)
                return entities.ToList();
            return entities.Take(request.Limit).ToList();
        }

        internal static ICollection<T> Evaluate<T>(this int limit, IEnumerable<T> entities)
        {
            if (limit < 1)
                return entities.ToList();
            return entities.Take(limit).ToList();
        }

        internal static ICollection<T> Evaluate<T>(this IEnumerable<Condition> conditions, IEnumerable<T> entities)
        {
            return entities.Where(conditions.ToPredicate<T>().Invoke).ToList();
        }

        internal static Predicate<T> ToPredicate<T>(this IEnumerable<Condition> conditions)
        {
            var type = typeof(T);
            if (type == typeof(DDictionary) || type.IsSubclassOf(typeof(DDictionary)))
            {
                throw new ArgumentException("ToPredicate() cannot be called on DDictionary types. Use " +
                                            "ToDDictionaryPredicate() instead");
            }

            var innerPredicates = conditions.Select(c =>
            {
                switch (c.Operator.Common)
                {
                    case "=":
                        return item =>
                        {
                            try
                            {
                                dynamic value;
                                var val1 = ExtensionMethods.GetValueFromKeyString(type, c.Key, item, out value);
                                return value == c.Value;
                            }
                            catch
                            {
                                return false;
                            }
                        };
                    case "!=":
                        return item =>
                        {
                            try
                            {
                                dynamic value;
                                var val1 = ExtensionMethods.GetValueFromKeyString(type, c.Key, item, out value);
                                return value != c.Value;
                            }
                            catch
                            {
                                return false;
                            }
                        };
                    case "<":
                        return item =>
                        {
                            try
                            {
                                dynamic value;
                                var val1 = ExtensionMethods.GetValueFromKeyString(type, c.Key, item, out value);
                                return value < c.Value;
                            }
                            catch
                            {
                                return false;
                            }
                        };

                    case ">":
                        return item =>
                        {
                            try
                            {
                                dynamic value;
                                var val1 = ExtensionMethods.GetValueFromKeyString(type, c.Key, item, out value);
                                return value > c.Value;
                            }
                            catch
                            {
                                return false;
                            }
                        };

                    case ">=":
                        return item =>
                        {
                            try
                            {
                                dynamic value;
                                var val1 = ExtensionMethods.GetValueFromKeyString(type, c.Key, item, out value);
                                return value >= c.Value;
                            }
                            catch
                            {
                                return false;
                            }
                        };

                    case "<=":
                        return item =>
                        {
                            try
                            {
                                dynamic value;
                                var val1 = ExtensionMethods.GetValueFromKeyString(type, c.Key, item, out value);
                                return value <= c.Value;
                            }
                            catch
                            {
                                return false;
                            }
                        };
                }

                return default(Predicate<T>);
            }).ToList();

            return item => innerPredicates.All(p => p != null && p(item));
        }
    }
}