﻿using System.Collections.Generic;
using System.Linq;
using RESTable.Internal;

namespace RESTable.Resources
{
    internal static class MethodsExtensions
    {
        internal static IReadOnlyList<Method> ResolveMethodRestrictions(this IEnumerable<Method> methods)
        {
            var methodsSet = methods?.ToHashSet();
            if (methodsSet == null || methodsSet.Count == 0)
                methodsSet = RESTableConfig.Methods.ToHashSet();
            if (methodsSet.Contains(Method.GET))
            {
                methodsSet.Add(Method.REPORT);
                methodsSet.Add(Method.HEAD);
            }
            return methodsSet.OrderBy(i => i, MethodComparer.Instance).ToList().AsReadOnly();
        }
    }
}