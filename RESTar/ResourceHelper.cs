using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Internal;

namespace RESTar
{
    public static class ResourceHelper
    {
        internal static void AutoMakeResource(Type type)
        {
            var attr = type.GetAttribute<RESTarAttribute>();
            if (attr == null)
                throw new ArgumentException("Can't automake without RESTarAttribute");
            var method = typeof(Resource<>).MakeGenericType(type)
                .GetMethod("Make", BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(type);
            method.Invoke(null, new object[] {attr.AvailableMethods, null, false});
        }

        public static void Register<T>(
            RESTarPresets preset,
            params RESTarMethods[] additionalMethods
        ) where T : class
        {
            var methods = preset.ToMethods().Union(additionalMethods).ToArray();
            Register<T>(methods.First(), methods.Length > 1 ? methods.Skip(1).ToArray() : null);
        }

        public static void Register<T>(
            RESTarMethods method,
            params RESTarMethods[] addMethods
        ) where T : class
        {
            var methods = new[] {method}.Union(addMethods).ToArray();
            Register<T>(methods.First(), methods.Length > 1 ? methods.Skip(1).ToArray() : null, null);
        }

        public static void Register<T>(
            RESTarPresets preset,
            IEnumerable<RESTarMethods> additionalMethods = null,
            OperationsProvider<T> operationsProvider = null
        ) where T : class
        {
            var methods = preset.ToMethods().Union(additionalMethods ?? new RESTarMethods[0]).ToArray();
            Register<T>(methods.First(), methods.Length > 1 ? methods.Skip(1).ToArray() : null, operationsProvider);
        }

        public static void Register<T>(
            RESTarMethods method,
            IEnumerable<RESTarMethods> addMethods = null,
            OperationsProvider<T> operationsProvider = null
        ) where T : class
        {
            if (typeof(T).HasAttribute<RESTarAttribute>())
                throw new InvalidOperationException("Cannot manually register resources that have a RESTar " +
                                                    "attribute. Resources decorated with a RESTar attribute " +
                                                    "are registered automatically");
            var availableMethods = new[] {method}.Union(addMethods ?? new RESTarMethods[0]).ToList();
            Resource<T>.Make(availableMethods, operationsProvider);
        }
    }
}