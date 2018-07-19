using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RESTar.Internal;
using RESTar.Meta;
using RESTar.Meta.Internal;
using RESTar.Requests;
using RESTar.Resources.Operations;

namespace RESTar.Resources
{
    /// <inheritdoc cref="EventArgs" />
    /// <inheritdoc cref="IEventInternal{T}" />
    public abstract class Event<T> : EventArgs, IEventInternal<T> where T : class
    {
        string IEventInternal<T>.Name => GetType().RESTarTypeName();
        ContentType? IEventInternal<T>.NativeContentType => ContentType;
        bool IEventInternal<T>.HasBinaryPayload => HasBinaryPayload;
        private bool HasBinaryPayload { get; }

        /// <summary>
        /// The payload of the event
        /// </summary>
        public T Payload { get; }

        /// <summary>
        /// The content type of the payload
        /// </summary>
        public ContentType? ContentType { get; }

        /// <inheritdoc />
        /// <summary>
        /// Creates a new wrapped event, with a given object as payload
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="contentType">
        /// Optionally, define a content type that is included together with the payload. If
        /// the payload is binary data (byte array or stream), this content type is needed
        /// for interpreting the payload.
        /// </param>
        protected Event(T payload, ContentType? contentType = null)
        {
            switch (payload)
            {
                case string _:
                    if (!contentType.HasValue)
                        contentType = Requests.ContentType.Parse("text/plain");
                    HasBinaryPayload = true;
                    break;
                case byte[] _:
                case Stream _:
                    if (!contentType.HasValue)
                        throw new InvalidOperationException(
                            $"Missing content type for event of type '{GetType()}'. A binary " +
                            "payload was used, but the 'contentType' parameter was null");
                    HasBinaryPayload = true;
                    break;
            }
            Payload = payload;
            ContentType = contentType;
        }

        /// <summary>
        /// Raises the event
        /// </summary>
        protected async void InvokeRaise() => await EventController.Raise(this);

        /// <inheritdoc />
        public virtual void Dispose() => (Payload as IDisposable)?.Dispose();

        /// <summary>
        /// The event handler for custom RESTar events of type T, subclasses of <see cref="Event{T}"/>.
        /// Use this to add listeners for RESTar custom events.
        /// </summary>
        public static event EventHandler<T> Raise
        {
            add
            {
                if (!typeof(IEvent).IsAssignableFrom(typeof(T)))
                    throw new InvalidOperationException($"Cannot add RESTar event handler for type '{typeof(T).RESTarTypeName()}'. Not a " +
                                                        "RESTar event type.");
                _Raise += value;
            }
            remove
            {
                if (!typeof(IEvent).IsAssignableFrom(typeof(T)))
                    throw new InvalidOperationException($"Cannot remove RESTar event handler for type '{typeof(T).RESTarTypeName()}'. Not a " +
                                                        "RESTar event type.");
                _Raise += value;
            }
        }

        /// <summary>
        /// Entity processors added to this event are invoked, in the order they are added, when the given entity resource's
        /// Inserter calls GetInputEntities() for the request, just before control is returned to the Inserter. The first
        /// delegate added to this event gets the output from GetInputEntities() as argument. Any subsequent delegates get
        /// the output from the previous delegate as input.
        /// </summary>
        public static event EntityProcessor<T> OnInsert
        {
            add
            {
                if (!(Resource<T>.SafeGet is IEntityResource))
                    throw new InvalidOperationException($"Cannot add event handler for type '{typeof(T).RESTarTypeName()}'. Not an entity resource.");
                _OnInsert += value;
            }
            remove
            {
                if (!(Resource<T>.SafeGet is IEntityResource))
                    throw new InvalidOperationException(
                        $"Cannot remove event handler for type '{typeof(T).RESTarTypeName()}'. Not an entity resource.");
                _OnInsert -= value;
            }
        }

        /// <summary>
        /// Entity processors added to this event are invoked, in the order they are added, when the given entity resource's
        /// Updater calls GetInputEntities() for the request, after the update operation is performed, just before the control
        /// is returned to the Updater. The first delegate added to this event gets the output from GetInputEntities(), i.e. the
        /// just updated entities, as argument. Any subsequent delegates get the output from the previous delegate as input.
        /// </summary>
        public static event EntityProcessor<T> OnUpdate
        {
            add
            {
                if (!(Resource<T>.SafeGet is IEntityResource))
                    throw new InvalidOperationException($"Cannot add event handler for type '{typeof(T).RESTarTypeName()}'. Not an entity resource.");
                _OnUpdate += value;
            }
            remove
            {
                if (!(Resource<T>.SafeGet is IEntityResource))
                    throw new InvalidOperationException(
                        $"Cannot remove event handler for type '{typeof(T).RESTarTypeName()}'. Not an entity resource.");
                _OnUpdate -= value;
            }
        }

        /// <summary>
        /// Entity processors added to this event are invoked, in the order they are added, when the given entity resource's
        /// Deleter calls GetInputEntities() for the request, just before the control is returned to the Deleter. The first
        /// delegate added to this event gets the output from GetInputEntities() as argument. Any subsequent delegates get the
        /// output from the previous delegate as input.
        /// </summary>
        public static event EntityProcessor<T> OnDelete
        {
            add
            {
                if (!(Resource<T>.SafeGet is IEntityResource))
                    throw new InvalidOperationException($"Cannot add event handler for type '{typeof(T).RESTarTypeName()}'. Not an entity resource.");
                _OnDelete += value;
            }
            remove
            {
                if (!(Resource<T>.SafeGet is IEntityResource))
                    throw new InvalidOperationException(
                        $"Cannot remove event handler for type '{typeof(T).RESTarTypeName()}'. Not an entity resource.");
                _OnDelete -= value;
            }
        }

        #region Internal

        private static event EventHandler<T> _Raise;
        private static event EntityProcessor<T> _OnInsert;
        private static event EntityProcessor<T> _OnUpdate;
        private static event EntityProcessor<T> _OnDelete;

        internal static void InvokeRaise(object sender, T @event)
        {
            _Raise?.Invoke(sender, @event);
        }

        internal static IEnumerable<T> InvokeOnInsert(IEnumerable<T> entities)
        {
            if (entities == null) return null;
            if (_OnInsert == null) return entities;
            return _OnInsert?.GetInvocationList()
                .OfType<EntityProcessor<T>>()
                .Aggregate(entities, (e, processor) => processor(e));
        }

        internal static IEnumerable<T> InvokeOnUpdate(IEnumerable<T> entities)
        {
            if (entities == null) return null;
            if (_OnUpdate == null) return entities;
            return _OnUpdate?.GetInvocationList()
                .OfType<EntityProcessor<T>>()
                .Aggregate(entities, (e, processor) => processor(e));
        }

        internal static IEnumerable<T> InvokeOnDelete(IEnumerable<T> entities)
        {
            if (entities == null) return null;
            if (_OnDelete == null) return entities;
            return _OnDelete?.GetInvocationList()
                .OfType<EntityProcessor<T>>()
                .Aggregate(entities, (e, processor) => processor(e));
        }

        #endregion
    }
}