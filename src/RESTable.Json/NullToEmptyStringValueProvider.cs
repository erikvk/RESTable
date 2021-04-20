using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace RESTable.Json
{
    /// <inheritdoc />
    /// <summary>
    /// Makes the empty string act as the default for strings in view models
    /// </summary>
    internal class NullToEmptyStringValueProvider : IValueProvider
    {
        private readonly PropertyInfo Property;
        public NullToEmptyStringValueProvider(PropertyInfo property) => Property = property;
        public object GetValue(object target) => Property.GetValue(target) ?? "";
        public void SetValue(object target, object value) => Property.SetValue(target, value);
    }
}