using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Internal;
using RESTar.Operations;
using static System.Reflection.BindingFlags;

namespace RESTar
{
    public static class Registrator
    {
        private static readonly MethodInfo AUTO_MAKER = typeof(Registrator)
            .GetMethod(nameof(AUTO_MAKE), NonPublic | Static);

        internal static void AutoMakeResource(Type type) => AUTO_MAKER
            .MakeGenericMethod(type)
            .Invoke(null, null);

        private static void AUTO_MAKE<T>() where T : class => Resource<T>
            .Make<T>(typeof(T)
                .GetAttribute<RESTarAttribute>()
                .AvailableMethods);

        public static void Register<T>(RESTarPresets preset, params RESTarMethods[] additionalMethods) where T : class
        {
            var methods = preset.ToMethods().Union(additionalMethods).ToArray();
            Register<T>(methods.First(), methods.Length > 1 ? methods.Skip(1).ToArray() : null);
        }

        public static void Register<T>(RESTarMethods method, params RESTarMethods[] addMethods) where T : class
        {
            var methods = new[] {method}.Union(addMethods).ToArray();
            Register<T>(methods.First(), methods.Length > 1 ? methods.Skip(1).ToArray() : null, null);
        }

        public static void Register<T>
        (
            RESTarPresets preset,
            IEnumerable<RESTarMethods> addMethods = null,
            Selector<T> selector = null,
            Inserter<T> inserter = null,
            Updater<T> updater = null,
            Deleter<T> deleter = null
        ) where T : class
        {
            var methods = preset.ToMethods().Union(addMethods ?? new RESTarMethods[0]).ToArray();
            Register
            (
                method: methods.First(),
                addMethods: methods.Length > 1
                    ? methods.Skip(1).ToArray()
                    : null,
                selector: selector,
                inserter: inserter,
                updater: updater,
                deleter: deleter
            );
        }

        public static void Register<T>
        (
            RESTarMethods method,
            IEnumerable<RESTarMethods> addMethods = null,
            Selector<T> selector = null,
            Inserter<T> inserter = null,
            Updater<T> updater = null,
            Deleter<T> deleter = null
        ) where T : class
        {
            if (typeof(T).HasAttribute<RESTarAttribute>())
                throw new InvalidOperationException("Cannot manually register resources that have a RESTar " +
                                                    "attribute. Resources decorated with a RESTar attribute " +
                                                    "are registered automatically");
            var availableMethods = new[] {method}.Union(addMethods ?? new RESTarMethods[0]).ToArray();
            Resource<T>.Make(availableMethods, selector, inserter, updater, deleter);
        }
    }
}