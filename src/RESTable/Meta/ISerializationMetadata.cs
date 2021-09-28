using System;

namespace RESTable.Meta
{
    public readonly ref struct DeclaredPropertyAssignment
    {
        private bool HasValue { get; }
        private object? Value { get; }

        public DeclaredPropertyAssignment(bool hasValue, object? value)
        {
            HasValue = hasValue;
            Value = value;
        }
    }

    public interface ISerializationMetadata
    {
        bool UsesParameterizedConstructor { get; }
        int ParameterizedConstructorParameterCount { get; }
        object InvokeParameterlessConstructor();
        object InvokeParameterizedConstructor((DeclaredProperty? declaredProperty, object? value)[] declaredPropertyValues);
        Type Type { get; }
        DeclaredProperty[] PropertiesToSerialize { get; }
        DeclaredProperty? GetProperty(string name);
        int DeclaredPropertyCount { get; }
        bool TypeIsDictionary { get; }
        Type? DictionaryValueType { get; }
        bool TypeIsWritableDictionary { get; }
        bool TypeIsNonDictionaryEnumerable { get; }
        bool TypeIsAsyncEnumerable { get; }
    }

    public interface ISerializationMetadata<out T> : ISerializationMetadata
    {
        new T InvokeParameterlessConstructor();
        new T InvokeParameterizedConstructor((DeclaredProperty? declaredProperty, object? value)[] declaredPropertyValues);
    }
}