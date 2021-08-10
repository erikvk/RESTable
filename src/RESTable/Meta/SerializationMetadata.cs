using System;
using System.Collections.Generic;
using System.Linq;

namespace RESTable.Meta
{
    internal class SerializationMetadata<T> : ISerializationMetadata<T>
    {
        private Constructor<T>? ParameterLessConstructor { get; }
        private IReadOnlyDictionary<string, DeclaredProperty> DeclaredProperties { get; }

        public DeclaredProperty[] PropertiesToSerialize { get; }

        public DeclaredProperty? GetProperty(string name)
        {
            DeclaredProperties.TryGetValue(name, out var property);
            return property;
        }

        public bool HasParameterLessConstructor => ParameterLessConstructor is not null;

        public bool TypeIsDictionary { get; }

        public bool TypeIsWritableDictionary { get; }

        object? ISerializationMetadata.CreateInstance() => CreateInstance();

        public T CreateInstance()
        {
            if (ParameterLessConstructor is not null)
                return ParameterLessConstructor();
            throw new InvalidOperationException($"Cannot create an instance of '{typeof(T).GetRESTableTypeName()}'. " +
                                                "The type is missing a parameterless constructor.");
        }

        public SerializationMetadata(TypeCache typeCache)
        {
            DeclaredProperties = typeCache.GetDeclaredProperties(typeof(T));
            PropertiesToSerialize = DeclaredProperties.Values
                .Where(p => !p.Hidden)
                .OrderBy(p => p.Order)
                .ToArray();
            TypeIsDictionary = typeof(T).IsDictionary(out var isWritable);
            TypeIsWritableDictionary = isWritable;
            ParameterLessConstructor = typeof(T).MakeStaticConstructor<T>();
        }
    }
}