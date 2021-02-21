using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace RESTable.ContentTypeProviders.NativeJsonProtocol
{
    /// <inheritdoc />
    /// <summary>
    /// Tries to get the value of a value type, and returns an empty object if the value is null
    /// </summary>
    internal class ClassNullToEmptyObjectProvider : IValueProvider
    {
        private readonly PropertyInfo Property;
        public ClassNullToEmptyObjectProvider(PropertyInfo property) => Property = property;
        public object GetValue(object target) => Property.GetValue(target) ?? new object();
        public void SetValue(object target, object value) => Property.SetValue(target, value);
    }
}