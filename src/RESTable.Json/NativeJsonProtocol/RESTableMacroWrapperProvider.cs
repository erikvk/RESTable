using System;
using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace RESTable.Json.NativeJsonProtocol
{
    /// <inheritdoc />
    /// <summary>
    /// Wraps an object in the RESTable macro for delivery to the view model
    /// </summary>
    internal class RESTableMacroWrapperProvider : IValueProvider
    {
        private readonly PropertyInfo Property;
        public RESTableMacroWrapperProvider(PropertyInfo property) => Property = property;
        public void SetValue(object target, object value) { }

        public object GetValue(object target)
        {
            switch (Property.GetValue(target))
            {
                case DateTime dateTime: return $"@RESTable(\"{dateTime:O}\")";
                case string @string: return $"@RESTable(\"{@string}\")";
                case var other: return $"@RESTable({other})";
            }
        }
    }
}