using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;

namespace RESTar.Resources
{
    internal static class MethodsExtensions
    {
        internal static IReadOnlyList<Method> ResolveMethodsCollection(this IEnumerable<Method> methods)
        {
            var methodRestrictions = methods.Distinct().ToArray();
            if (!methodRestrictions.Any())
                methodRestrictions = RESTarConfig.Methods;
            var restrictions = methodRestrictions.OrderBy(i => i, MethodComparer.Instance).ToList();
            if (restrictions.Contains(Method.GET) && !restrictions.Contains(Method.REPORT))
                restrictions.Add(Method.REPORT);
            if (restrictions.Contains(Method.GET) && !restrictions.Contains(Method.HEAD))
                restrictions.Add(Method.HEAD);
            return restrictions.ToList().AsReadOnly();
        }
    }
}