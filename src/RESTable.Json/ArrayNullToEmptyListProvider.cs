using System;
using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace RESTable.Json
{
    /// <inheritdoc />
    /// <summary>
    /// Tries to get the value of a collection, and returns an empty array if the value is null
    /// </summary>
    internal class ArrayNullToEmptyListProvider : IValueProvider
    {
        private readonly PropertyInfo Property;
        public ArrayNullToEmptyListProvider(PropertyInfo property) => Property = property;
        public object GetValue(object target) => Property.GetValue(target) ?? Array.Empty<object>();
        public void SetValue(object target, object value) => Property.SetValue(target, value);
    }
}