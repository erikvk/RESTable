using System;
using System.IO;
using RESTar.Internal;
using RESTar.Meta.Internal;
using RESTar.Requests;

namespace RESTar.Resources
{
    /// <inheritdoc />
    public abstract class EventWrapper<T> : IEventInternal where T : class
    {
        object IEventInternal.Payload => Payload;
        string IEventInternal.Name => GetType().RESTarTypeName();
        ContentType? IEventInternal.NativeContentType => ContentType;
        bool IEventInternal.HasBinaryPayload => HasBinaryPayload;
        private bool HasBinaryPayload { get; }

        /// <summary>
        /// The payload of the event
        /// </summary>
        public T Payload { get; }

        /// <summary>
        /// The content type of the payload
        /// </summary>
        public ContentType? ContentType { get; }

        /// <summary>
        /// Creates a new wrapped event, with a given object as payload
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="contentType">
        /// Optionally, define a content type that is included together with the payload. If
        /// the payload is binary data (byte array or stream), this content type is needed
        /// for interpreting the payload.
        /// </param>
        protected EventWrapper(T payload, ContentType? contentType = null)
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
        protected async void Raise() => await EventController.Raise(this);

        /// <inheritdoc />
        public virtual void Dispose()
        {
            if (Payload is IDisposable disposable)
                disposable.Dispose();
        }
    }
}