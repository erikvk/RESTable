using System;
using System.IO;
using System.Threading.Tasks;
using RESTar.Internal;
using RESTar.Meta.Internal;
using RESTar.Requests;

namespace RESTar.Resources
{
    /// <summary>
    /// The generic base type for RESTar event types.
    /// </summary>
    /// <inheritdoc cref="EventArgs" />
    /// <inheritdoc cref="IEventInternal{T}" />
    /// <typeparam name="TPayload">The payload type, for custom RESTar events, or the entity resource
    /// type when working with the static events</typeparam>
    public abstract class Event<TPayload> : EventArgs, IEventInternal<TPayload> where TPayload : class
    {
        string IEventInternal<TPayload>.Name => GetType().RESTarTypeName();
        ContentType? IEventInternal<TPayload>.NativeContentType => ContentType;
        bool IEventInternal<TPayload>.HasBinaryPayload => HasBinaryPayload;

        private bool HasBinaryPayload { get; }

        /// <summary>
        /// The payload of the event
        /// </summary>
        public TPayload Payload { get; }

        /// <summary>
        /// The content type of the payload
        /// </summary>
        public ContentType? ContentType { get; }

        /// <inheritdoc />
        /// <summary>
        /// Creates a new event, with a given object as payload
        /// </summary>
        /// <param name="payload">The payload to use </param>
        /// <param name="contentType">
        /// Optionally, define a content type that is included together with the payload. If
        /// the payload is binary data (byte array or stream), this content type is needed
        /// for interpreting the payload.
        /// </param>
        protected Event(TPayload payload, ContentType? contentType = null)
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
                case null: throw new ArgumentNullException(nameof(payload), "Event payload cannot be null");
            }
            Payload = payload;
            ContentType = contentType;
        }

        /// <summary>
        /// Raises the event
        /// </summary>
        protected async void Raise() => await Raiser((dynamic) this);

        private static async Task Raiser<TEvent>(TEvent @event) where TEvent : Event<TPayload>
        {
            if (!RESTarConfig.Initialized) return;
            if (Meta.EventResource<TEvent, TPayload>.SafeGet == null)
                throw new UnknownEventTypeException(@event);
            var hookTask = WebhookController.Post(@event);
            Events.Custom<TEvent>.OnRaise(@event);
            await hookTask;
        }

        /// <inheritdoc />
        public virtual void Dispose() => (Payload as IDisposable)?.Dispose();
    }
}