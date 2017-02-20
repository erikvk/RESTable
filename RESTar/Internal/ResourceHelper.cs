using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Starcounter;

namespace RESTar.Internal
{
    internal static class ResourceHelper
    {
        internal static void AutoMakeResource(Type type)
        {
            var attr = type.GetAttribute<RESTarAttribute>();
            if (attr == null)
                throw new ArgumentException("Can't automake without RESTarAttribute");
            var method = typeof(Resource<>).MakeGenericType(type).GetMethod("Make", BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(type);
            method.Invoke(null, new object[] {attr.AvailableMethods, null, false});
        }
    }
}