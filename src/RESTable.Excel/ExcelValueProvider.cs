using System;

namespace RESTable.Excel
{
    internal readonly struct ExcelValueProvider : IValueProvider
    {
        private object? ExcelCell { get; }

        public T? GetValue<T>()
        {
            if (ExcelCell is null)
                return default;
            if (ExcelCell is T tValue)
                return tValue;
            throw new InvalidCastException($"Cannot convert object of type '{ExcelCell.GetType()}' to '{typeof(T).GetRESTableTypeName()}'");
        }

        public object? GetValue(Type targetType) => ExcelCell;

        public ExcelValueProvider(object? excelCell)
        {
            ExcelCell = excelCell;
        }
    }
}