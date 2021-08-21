using System;

namespace RESTable.Meta
{
    internal class SerializationMetadataAccessor : ISerializationMetadataAccessor
    {
        private Func<Type, ISerializationMetadata> Accessor { get; }
        public SerializationMetadataAccessor(Func<Type, ISerializationMetadata> accessor) => Accessor = accessor;
        public ISerializationMetadata<T> GetMetadata<T>() => (ISerializationMetadata<T>) Accessor(typeof(T));
        public ISerializationMetadata<T> GetMetadata<T>(T instance) => (ISerializationMetadata<T>) Accessor(typeof(T));
        public ISerializationMetadata GetMetadata(Type toSerialize) => Accessor(toSerialize);
    }
}