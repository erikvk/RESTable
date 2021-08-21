using System;

namespace RESTable.Meta
{
    public interface ISerializationMetadata
    {
        bool HasParameterLessConstructor { get; }
        object? CreateInstance();
        Type Type { get; }
        DeclaredProperty[] PropertiesToSerialize { get; }
        DeclaredProperty? GetProperty(string name);
        bool TypeIsDictionary { get; }
        bool TypeIsWritableDictionary { get; }
    }

    public interface ISerializationMetadata<out T> : ISerializationMetadata
    {
        new T CreateInstance();
    }
}