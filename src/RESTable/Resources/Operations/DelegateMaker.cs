using System;
using System.Linq;

namespace RESTable.Resources.Operations
{
    internal static class DelegateMaker
    {
        private static Type GetMatchingInterface<TDelegate>() where TDelegate : Delegate
        {
            var t = typeof(TDelegate).GetGenericArguments().ElementAt(0);
            return typeof(TDelegate) switch
            {
                var d when d == typeof(Selector<>).MakeGenericType(t) => typeof(ISelector<>).MakeGenericType(t),
                var d when d == typeof(Inserter<>).MakeGenericType(t) => typeof(IInserter<>).MakeGenericType(t),
                var d when d == typeof(Updater<>).MakeGenericType(t) => typeof(IUpdater<>).MakeGenericType(t),
                var d when d == typeof(Deleter<>).MakeGenericType(t) => typeof(IDeleter<>).MakeGenericType(t),
                var d when d == typeof(Counter<>).MakeGenericType(t) => typeof(ICounter<>).MakeGenericType(t),
                var d when d == typeof(Authenticator<>).MakeGenericType(t) => typeof(IAuthenticatable<>).MakeGenericType(t),
                var d when d == typeof(BinarySelector<>).MakeGenericType(t) => typeof(IBinary<>).MakeGenericType(t),
                var d when d == typeof(ViewSelector<>).MakeGenericType(t) => typeof(ISelector<>).MakeGenericType(t),

                var d when d == typeof(Validator<>).MakeGenericType(t) => typeof(IValidator<>).MakeGenericType(t),

                var d when d == typeof(AsyncSelector<>).MakeGenericType(t) => typeof(IAsyncSelector<>).MakeGenericType(t),
                var d when d == typeof(AsyncInserter<>).MakeGenericType(t) => typeof(IAsyncInserter<>).MakeGenericType(t),
                var d when d == typeof(AsyncUpdater<>).MakeGenericType(t) => typeof(IAsyncUpdater<>).MakeGenericType(t),
                var d when d == typeof(AsyncDeleter<>).MakeGenericType(t) => typeof(IAsyncDeleter<>).MakeGenericType(t),
                var d when d == typeof(AsyncCounter<>).MakeGenericType(t) => typeof(IAsyncCounter<>).MakeGenericType(t),
                var d when d == typeof(AsyncAuthenticator<>).MakeGenericType(t) => typeof(IAsyncAuthenticatable<>).MakeGenericType(t),
                var d when d == typeof(BinarySelector<>).MakeGenericType(t) => typeof(IBinary<>).MakeGenericType(t),
                var d when d == typeof(AsyncViewSelector<>).MakeGenericType(t) => typeof(IAsyncSelector<>).MakeGenericType(t),

                _ => throw new ArgumentOutOfRangeException()
            };
        }

        internal static (Type sync, Type async) GetMatchingInterface(RESTableOperations operation) => operation switch
        {
            RESTableOperations.Select => (sync: typeof(Selector<>), async: typeof(IAsyncSelector<>)),
            RESTableOperations.Insert => (sync: typeof(Inserter<>), async: typeof(IAsyncInserter<>)),
            RESTableOperations.Update => (sync: typeof(Updater<>), async: typeof(IAsyncUpdater<>)),
            RESTableOperations.Delete => (sync: typeof(Deleter<>), async: typeof(IAsyncDeleter<>)),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };

        /// <summary>
        /// Gets the given operations delegate from a given resource type definition
        /// </summary>
        internal static TDelegate? GetDelegate<TDelegate>(Type target) where TDelegate : Delegate
        {
            var i = GetMatchingInterface<TDelegate>();
            if (!i.IsAssignableFrom(target)) return default;
            var method = target.GetInterfaceMap(i).TargetMethods.First();
            if (method.DeclaringType != target)
            {
                if (target.GetConstructor(Type.EmptyTypes) is null)
                    throw new InvalidResourceDeclarationException($"Invalid resource declaration for type '{target}'. Any resource " +
                                                                  "type inheriting operation implementations from a generic base " +
                                                                  "class must define a public parameterless constructor, due to " +
                                                                  "limitations in how CLR can bind delegates to these types.");
                var instance = Activator.CreateInstance(target);
                return (TDelegate) Delegate.CreateDelegate(typeof(TDelegate), instance, method);
            }
            return (TDelegate) Delegate.CreateDelegate(typeof(TDelegate), null, method);
        }
    }
}