using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Admin;
using RESTar.Internal;

namespace RESTar.Operations
{
    internal static class DelegateMaker
    {
        private static Type MatchingInterface<TDelegate, TResource>() where TResource : class
        {
            switch (typeof(TDelegate))
            {
                case var d when d == typeof(Selector<TResource>): return typeof(ISelector<TResource>);
                case var d when d == typeof(Inserter<TResource>): return typeof(IInserter<TResource>);
                case var d when d == typeof(Updater<TResource>): return typeof(IUpdater<TResource>);
                case var d when d == typeof(Deleter<TResource>): return typeof(IDeleter<TResource>);
                case var d when d == typeof(Counter<TResource>): return typeof(ICounter<TResource>);
                case var d when d == typeof(Profiler): return typeof(IProfiler);
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
        internal static TDelegate GetDelegate<TDelegate, TResource>() where TResource : class => typeof(TResource)
            .SafeGet(t => t.GetInterfaceMap(MatchingInterface<TDelegate, TResource>()))
            .TargetMethods?
            .FirstOrDefault()?
            .MakeDelegate<TDelegate>();
    }

    /// <summary>
    /// Specifies the Select operation used in GET, PATCH, PUT and DELETE. Select gets a set 
    /// of entities from a resource that satisfy certain conditions provided in the request, 
    /// and returns them.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    public delegate IEnumerable<T> Selector<T>(IRequest<T> request) where T : class;

    /// <summary>
    /// Specifies the Insert operation used in POST and PUT. Takes a set of entities and inserts 
    /// them into the resource, and returns the number of entities successfully inserted.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    public delegate int Inserter<T>(IEnumerable<T> entities, IRequest<T> request) where T : class;

    /// <summary>
    /// Specifies the Update operation used in PATCH and PUT. Takes a set of entities and updates 
    /// their corresponding entities in the resource (often by deleting the old ones and inserting 
    /// the new), and returns the number of entities successfully updated.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    public delegate int Updater<T>(IEnumerable<T> entities, IRequest<T> request) where T : class;

    /// <summary>
    /// Specifies the Delete operation used in DELETE. Takes a set of entities and deletes them from 
    /// the resource, and returns the number of entities successfully deleted.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    public delegate int Deleter<T>(IEnumerable<T> entities, IRequest<T> request) where T : class;

    /// <summary>
    /// Counts the entities that satisfy certain conditions provided in the request
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    public delegate long Counter<T>(IRequest<T> request) where T : class;

    /// <summary>
    /// Generates a profile for a given resource
    /// </summary>
    public delegate ResourceProfile Profiler();

    internal delegate long ByteCounter<in T>(IEnumerable<T> objects);
}