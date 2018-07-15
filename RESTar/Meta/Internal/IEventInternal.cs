using System;
using RESTar.Requests;

namespace RESTar.Meta.Internal
{
    /// <summary>
    /// A common interface for event types
    /// </summary>
    public interface IEvent { }

    internal interface IEventInternal : IEvent, IDisposable
    {
        string Name { get; }
        object Payload { get; }
        ContentType? NativeContentType { get; }
        bool HasBinaryPayload { get; }
    }
}