using System;

namespace RESTable;

public interface IValueProvider
{
    T? GetValue<T>();
    object? GetValue(Type targetType);
}
