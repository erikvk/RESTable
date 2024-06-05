using System;

namespace RESTable;

public readonly struct PopulateSource
{
    private IValueProvider ValueProvider { get; }

    public SourceKind SourceKind { get; }

    public object? GetValue(Type targetType)
    {
        return ValueProvider.GetValue(targetType);
    }

    public T? GetValue<T>()
    {
        return ValueProvider.GetValue<T>();
    }

    public (string?, PopulateSource)[] Properties { get; }

    public PopulateSource(SourceKind sourceKind, IValueProvider valueProvider, (string, PopulateSource)[]? properties = null)
    {
        SourceKind = sourceKind;
        ValueProvider = valueProvider;
        if (properties is null)
            Properties = [];
        else Properties = properties!;
    }
}
