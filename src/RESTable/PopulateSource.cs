using System;

namespace RESTable
{
    public readonly struct PopulateSource
    {
        private IValueProvider ValueProvider { get; }

        public SourceKind SourceKind { get; }

        public object? GetValue(Type targetType) => ValueProvider.GetValue(targetType);

        public T? GetValue<T>() => ValueProvider.GetValue<T>();

        public (string?, PopulateSource)[] Properties { get; }

        public PopulateSource(SourceKind sourceKind, IValueProvider valueProvider, (string, PopulateSource)[]? properties = null)
        {
            SourceKind = sourceKind;
            ValueProvider = valueProvider;
            if (properties is null)
                Properties = Array.Empty<(string?, PopulateSource)>();
            else Properties = properties!;
        }
    }
}