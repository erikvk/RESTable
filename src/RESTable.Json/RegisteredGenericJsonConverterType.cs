using System;

namespace RESTable.Json
{
    internal class RegisteredGenericJsonConverterType : IRegisteredGenericJsonConverterType
    {
        private Predicate<Type> CanConvertDelegate { get; }
        private Type GenericConverterType { get; }

        public RegisteredGenericJsonConverterType(Type genericConverterType, Predicate<Type> canConvertDelegate)
        {
            GenericConverterType = genericConverterType;
            CanConvertDelegate = canConvertDelegate;
        }

        public bool CanConvert(Type toConvert) => CanConvertDelegate(toConvert);

        public Type GetConverterType(Type toConvert) => GenericConverterType.MakeGenericType(toConvert);
    }
}