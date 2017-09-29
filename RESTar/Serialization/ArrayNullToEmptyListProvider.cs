using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace RESTar.Serialization
{
    /// <inheritdoc />
    internal class ArrayNullToEmptyListProvider : IValueProvider
    {
        private readonly PropertyInfo Property;

        /// <summary>
        /// </summary>
        public ArrayNullToEmptyListProvider(PropertyInfo property)
        {
            Property = property;
        }

        /// <inheritdoc />
        public object GetValue(object target) => Property.GetValue(target) ?? new object[0];

        /// <inheritdoc />
        public void SetValue(object target, object value) => Property.SetValue(target, value);
    }
}