using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Admin;
using RESTar.Resources;

namespace RESTar.Operations
{
    internal static class DelegateMaker
    {
        private static Type MatchingInterface<TDelegate>()
        {
            var t = typeof(TDelegate).GetGenericArguments().ElementAtOrDefault(0);
            switch (typeof(TDelegate))
            {
                case var d when d == typeof(Selector<>).MakeGenericType(t): return typeof(ISelector<>).MakeGenericType(t);
                case var d when d == typeof(Inserter<>).MakeGenericType(t): return typeof(IInserter<>).MakeGenericType(t);
                case var d when d == typeof(Updater<>).MakeGenericType(t): return typeof(IUpdater<>).MakeGenericType(t);
                case var d when d == typeof(Deleter<>).MakeGenericType(t): return typeof(IDeleter<>).MakeGenericType(t);
                case var d when d == typeof(Counter<>).MakeGenericType(t): return typeof(ICounter<>).MakeGenericType(t);
                case var d when d == typeof(Profiler<>).MakeGenericType(t): return typeof(IProfiler<>).MakeGenericType(t);
                case var d when d == typeof(Authenticator<>).MakeGenericType(t): return typeof(IAuthenticatable<>).MakeGenericType(t);
                default: throw new ArgumentOutOfRangeException();
            }
        }

        internal static Type MatchingInterface(RESTarOperations operation)
        {
            switch (operation)
            {
                case RESTarOperations.Select: return typeof(ISelector<>);
                case RESTarOperations.Insert: return typeof(IInserter<>);
                case RESTarOperations.Update: return typeof(IUpdater<>);
                case RESTarOperations.Delete: return typeof(IDeleter<>);
                default: throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
            }
        }

        private static dynamic MakeDelegate<T>(this MethodInfo method) => method.CreateDelegate(typeof(T), null);

        /// <summary>
        /// Gets the given operations delegate from a given resource type definition
        /// </summary>
        internal static TDelegate GetDelegate<TDelegate>(Type target) => target
            .SafeGet(t => t.GetInterfaceMap(MatchingInterface<TDelegate>())
                .TargetMethods?
                .FirstOrDefault()?
                .MakeDelegate<TDelegate>());
    }

    /// <summary>
    /// Specifies the Select operation used in GET, PATCH, PUT and DELETE. Select gets a set 
    /// of entities from a resource that satisfy certain conditions provided in the request, 
    /// and returns them.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    public delegate IEnumerable<T> Selector<T>(IQuery<T> query) where T : class;

    /// <summary>
    /// Specifies the Insert operation used in POST and PUT. Takes a set of entities and inserts 
    /// them into the resource, and returns the number of entities successfully inserted.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    public delegate int Inserter<T>(IQuery<T> query) where T : class;

    /// <summary>
    /// Specifies the Update operation used in PATCH and PUT. Takes a set of entities and updates 
    /// their corresponding entities in the resource (often by deleting the old ones and inserting 
    /// the new), and returns the number of entities successfully updated.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    public delegate int Updater<T>(IQuery<T> query) where T : class;

    /// <summary>
    /// Specifies the Delete operation used in DELETE. Takes a set of entities and deletes them from 
    /// the resource, and returns the number of entities successfully deleted.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    public delegate int Deleter<T>(IQuery<T> query) where T : class;

    /// <summary>
    /// Counts the entities that satisfy certain conditions provided in the request
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    public delegate long Counter<T>(IQuery<T> query) where T : class;

    /// <summary>
    /// Generates a profile for a given resource
    /// </summary>
    public delegate ResourceProfile Profiler<T>(IEntityResource<T> resource) where T : class;

    /// <summary>
    /// Authenticates a request
    /// </summary>
    public delegate AuthResults Authenticator<T>(IQuery<T> query) where T : class;
}