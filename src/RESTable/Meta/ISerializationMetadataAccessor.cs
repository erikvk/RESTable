using System;

namespace RESTable.Meta;

public interface ISerializationMetadataAccessor
{
    ISerializationMetadata<T> GetMetadata<T>(T instance);
    ISerializationMetadata<T> GetMetadata<T>();
    ISerializationMetadata GetMetadata(Type toSerialize);
}