using System;
using RESTar.Internal;
using RESTar.Meta.Internal;
using RESTar.Requests;

namespace RESTar.Resources
{
    /// <inheritdoc cref="EventArgs" />
    /// <inheritdoc cref="IEventInternal" />
    public abstract class Event : EventArgs, IEventInternal
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
    }

    /// <summary>
    /// Provides access to event handlers for each RESTar event type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class Event<T> where T : EventArgs, IEvent
    {
        internal static void OnRaise(object sender, T @event) => Raise?.Invoke(sender, @event);

        /// <summary>
        /// The event handler for the type T
        /// </summary>
        public static event EventHandler<T> Raise;
    }
}