using System;
using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace RESTable.Json
{
    /// <inheritdoc />
    /// <summary>
    /// Wraps an object in the RESTable macro for delivery to the view model
    /// </summary>
    internal class RESTableMacroWrapperProvider : IValueProvider
    {
        private readonly PropertyInfo Property;
        public RESTableMacroWrapperProvider(PropertyInfo property) => Property = property;
        public void SetValue(object target, object? value) { }

        public object GetValue(object target) => Property.GetValue(target) switch
        {
            DateTime dateTime => $"@RESTable(\"{dateTime:O}\")",
            string @string => $"@RESTable(\"{@string}\")",
            var other => $"@RESTable({other})"
        };
    }
}