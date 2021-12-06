using System;

namespace RESTable.Json;

public interface IRegisteredGenericJsonConverterType
{
    bool CanConvert(Type toConvert);
    Type GetConverterType(Type toConvert);
}