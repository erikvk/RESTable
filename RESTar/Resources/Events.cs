using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Meta.Internal;
using RESTar.Resources.Operations;

namespace RESTar.Resources
{
    /// <summary>
    /// Provides static .NET events for RESTar types. 
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Provides static .NET events for entity resource types, and their operations. <see cref="PostInsert"/>,
        /// for example, is raised whenever an entity is inserted into the given entity resource.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static class EntityResource<T> where T : class
        {
            internal static IEnumerable<T> OnInsert(IEnumerable<T> entities)
            {
                if (entities == null || PostInsert == null) return entities;
                return PostInsert?.GetInvocationList()
                    .OfType<EntityProcessor<T>>()
                    .Aggregate(entities, (e, processor) => processor(e));
            }

            internal static IEnumerable<T> OnUpdate(IEnumerable<T> entities)
            {
                if (entities == null || PostInsert == null) return entities;
                return PostUpdate?.GetInvocationList()
                    .OfType<EntityProcessor<T>>()
                    .Aggregate(entities, (e, processor) => processor(e));
            }

            internal static IEnumerable<T> OnDelete(IEnumerable<T> entities)
            {
                if (entities == null || PostInsert == null) return entities;
                return PreDelete?.GetInvocationList()
                    .OfType<EntityProcessor<T>>()
                    .Aggregate(entities, (e, processor) => processor(e));
            }

            /// <summary>
            /// Entity processors added to this event are invoked, in the order they are added, when the given entity resource's
            /// Inserter calls GetInputEntities() for the request, just before control is returned to the Inserter. The first
            /// delegate added to this event gets the output from GetInputEntities() as argument. Any subsequent delegates get
            /// the output from the previous delegate as input.
            /// </summary>
            public static event EntityProcessor<T> PostInsert;

            /// <summary>
            /// Entity processors added to this event are invoked, in the order they are added, when the given entity resource's
            /// Updater calls GetInputEntities() for the request, after the update operation is performed, just before the control
            /// is returned to the Updater. The first delegate added to this event gets the output from GetInputEntities(), i.e. the
            /// just updated entities, as argument. Any subsequent delegates get the output from the previous delegate as input.
            /// </summary>
            public static event EntityProcessor<T> PostUpdate;

            /// <summary>
            /// Entity processors added to this event are invoked, in the order they are added, when the given entity resource's
            /// Deleter calls GetInputEntities() for the request, just before the control is returned to the Deleter. The first
            /// delegate added to this event gets the output from GetInputEntities() as argument. Any subsequent delegates get the
            /// output from the previous delegate as input.
            /// </summary>
            public static event EntityProcessor<T> PreDelete;
        }

        /// <summary>
        /// Provides static .NET events for custom RESTar events.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static class Custom<T> where T : class, IEvent
        {
            /// <summary>
            /// The event handler for custom RESTar events of type T, subclasses of <see cref="Event{T}"/>.
            /// Use this to add listeners for RESTar custom events.
            /// </summary>
            public static event EventHandler<T> Raise;

            internal static void OnRaise(T e) => Raise?.Invoke(null, e);
        }
    }
}