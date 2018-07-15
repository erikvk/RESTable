using RESTar.Internal;
using RESTar.Meta.Internal;
using RESTar.Requests;

namespace RESTar.Resources
{
    /// <inheritdoc />
    public abstract class Event : IEventInternal
    {
        object IEventInternal.Payload => this;
        string IEventInternal.Name => GetType().RESTarTypeName();
        ContentType? IEventInternal.NativeContentType => null;
        bool IEventInternal.HasBinaryPayload => false;

        /// <summary>
        /// Raises the event
        /// </summary>
        protected async void Raise() => await EventController.Raise(this);

        /// <inheritdoc />
        protected Event() { }

        /// <inheritdoc />
        public virtual void Dispose() { }

        internal static void RaiseHandlers<T>(T @event) where T : IEvent => Event<T>.Invoke(@event);
    }

    /// <summary>
    /// Describes a RESTar event handler.
    /// </summary>
    public delegate void RESTarEventHandler<in T>(T @event);

    /// <summary>
    /// Provides access to event handlers for each RESTar event type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class Event<T> where T : IEvent
    {
        internal static void Invoke(T @event) => Raise?.Invoke(@event);

        /// <summary>
        /// The event handler for the type T
        /// </summary>
        public static event RESTarEventHandler<T> Raise;
    }
}