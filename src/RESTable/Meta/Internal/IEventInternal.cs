using System;

namespace RESTable.Meta.Internal;

internal interface IEventInternal<out T> : IEvent, IDisposable where T : class
{
    string Name { get; }
    T Payload { get; }
    ContentType? NativeContentType { get; }
    bool HasBinaryPayload { get; }
}
