using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTable.Results;

namespace RESTable.Meta;

internal class SerializationMetadata<T> : ISerializationMetadata<T>
{
    public SerializationMetadata(TypeCache typeCache)
    {
        DeclaredProperties = typeCache.GetDeclaredProperties(typeof(T));
        PropertiesToSerialize = DeclaredProperties.Values
            .Where(p => !p.Hidden)
            .OrderBy(p => p.Order)
            .ToArray();
        if (typeof(T).IsDictionary(out var isWritable, out var dictionaryParameters))
        {
            TypeIsDictionary = true;
            TypeIsWritableDictionary = isWritable;
            DictionaryValueType = dictionaryParameters![1];
        }
        else
        {
            TypeIsDictionary = false;
            TypeIsWritableDictionary = false;
            DictionaryValueType = null;
        }
        TypeIsAsyncEnumerable = typeof(T).ImplementsGenericInterface(typeof(IAsyncEnumerable<>), out _);
        TypeIsNonDictionaryEnumerable = !TypeIsDictionary && !TypeIsAsyncEnumerable && typeof(T).ImplementsEnumerableInterface(out _);

        ParameterLessConstructor = typeof(T).MakeParameterlessConstructor<T>();
        if (typeof(T).GetCustomConstructor() is ConstructorInfo constructor && constructor.GetParameters() is {Length: > 0} parameters)
        {
            UsesParameterizedConstructor = true;
            CustomParameterizedConstructor = constructor.MakeParameterizedConstructor<T>();
            CustomParameterizedConstructorParameters = parameters;
            foreach (var parameter in CustomParameterizedConstructorParameters)
            {
                var parameterName = parameter.RESTableParameterName(TypeIsDictionary);
                if (!DeclaredProperties.TryGetValue(parameterName, out _))
                    throw new InvalidOperationException(
                        $"Invalid custom parameterized constructor for type '{typeof(T).GetRESTableTypeName()}'. Expected each constructor parameter to " +
                        $"have the same name (case insensitive) as a public instance property. Found parameter '{parameterName}' with no such matching property"
                    );
            }
        }
        else
        {
            UsesParameterizedConstructor = false;
            CustomParameterizedConstructorParameters = Array.Empty<ParameterInfo>();
        }
    }

    private ParameterlessConstructor<T>? ParameterLessConstructor { get; }
    private ParameterizedConstructor<T>? CustomParameterizedConstructor { get; }
    private ParameterInfo[] CustomParameterizedConstructorParameters { get; }
    private IReadOnlyDictionary<string, DeclaredProperty> DeclaredProperties { get; }

    public Type Type => typeof(T);
    public DeclaredProperty[] PropertiesToSerialize { get; }
    public int DeclaredPropertyCount => DeclaredProperties.Count;
    public bool UsesParameterizedConstructor { get; }
    public int ParameterizedConstructorParameterCount => CustomParameterizedConstructorParameters.Length;
    public bool TypeIsDictionary { get; }
    public bool TypeIsWritableDictionary { get; }
    public bool TypeIsNonDictionaryEnumerable { get; }
    public bool TypeIsAsyncEnumerable { get; }
    public Type? DictionaryValueType { get; }

    public DeclaredProperty? GetProperty(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (TypeIsDictionary && name[0] != '$') name = $"${name}";
        DeclaredProperties.TryGetValue(name, out var property);
        return property;
    }

    object ISerializationMetadata.InvokeParameterlessConstructor()
    {
        return InvokeParameterlessConstructor()!;
    }

    object ISerializationMetadata.InvokeParameterizedConstructor((DeclaredProperty? declaredProperty, object? value)[] declaredPropertyValues)
    {
        return InvokeParameterizedConstructor(declaredPropertyValues)!;
    }

    public T InvokeParameterlessConstructor()
    {
        if (ParameterLessConstructor is not null)
            return ParameterLessConstructor();
        throw new InvalidOperationException($"Cannot create an instance of '{typeof(T).GetRESTableTypeName()}'. " +
                                            "The type has no custom constructor marked with RESTableConstructorAttribute " +
                                            "or JsonConstructorAttribute, and is missing a parameterless constructor.");
    }

    public T InvokeParameterizedConstructor((DeclaredProperty? declaredProperty, object? value)[] declaredPropertyValues)
    {
        List<ParameterInfo>? missingParametersList = null;
        var parameterList = new object?[CustomParameterizedConstructorParameters.Length];
        for (var i = 0; i < parameterList.Length; i += 1)
        {
            var (property, value) = declaredPropertyValues[i];
            if (property is null)
            {
                var parameter = CustomParameterizedConstructorParameters[i];
                if (parameter.IsOptional)
                {
                    value = Missing.Value;
                }
                else
                {
                    missingParametersList ??= new List<ParameterInfo>();
                    missingParametersList.Add(parameter);
                }
            }
            parameterList[i] = value;
        }
        if (missingParametersList is not null)
        {
            var invalidMembers = missingParametersList.ToInvalidMembers(Type);
            throw new MissingConstructorParameter(Type, invalidMembers);
        }
        return CustomParameterizedConstructor!.Invoke(parameterList);
    }
}