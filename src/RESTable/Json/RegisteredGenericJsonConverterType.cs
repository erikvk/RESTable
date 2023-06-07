using System;

namespace RESTable.Json;

internal class RegisteredGenericJsonConverterType : IRegisteredGenericJsonConverterType
{
    public RegisteredGenericJsonConverterType(Type genericConverterType, Predicate<Type> canConvertDelegate)
    {
        GenericConverterType = genericConverterType;
        CanConvertDelegate = canConvertDelegate;
    }

    private Predicate<Type> CanConvertDelegate { get; }
    private Type GenericConverterType { get; }

    public bool CanConvert(Type toConvert)
    {
        return CanConvertDelegate(toConvert);
    }

    public Type GetConverterType(Type toConvert)
    {
        return GenericConverterType.MakeGenericType(toConvert);
    }
}
