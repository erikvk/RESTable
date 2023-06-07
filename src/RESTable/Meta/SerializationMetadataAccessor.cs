using System;

namespace RESTable.Meta;

internal class SerializationMetadataAccessor : ISerializationMetadataAccessor
{
    public SerializationMetadataAccessor(Func<Type, ISerializationMetadata> accessor)
    {
        Accessor = accessor;
    }

    private Func<Type, ISerializationMetadata> Accessor { get; }

    public ISerializationMetadata<T> GetMetadata<T>()
    {
        return (ISerializationMetadata<T>) Accessor(typeof(T));
    }

    public ISerializationMetadata<T> GetMetadata<T>(T instance)
    {
        return (ISerializationMetadata<T>) Accessor(typeof(T));
    }

    public ISerializationMetadata GetMetadata(Type toSerialize)
    {
        return Accessor(toSerialize);
    }
}
