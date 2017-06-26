using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace RESTar.View.Serializer
{
    /// <summary>
    /// </summary>
    internal class ArrayNullToEmptyListProvider : IValueProvider
    {
        private readonly PropertyInfo Property;

        /// <summary>
        /// </summary>
        public ArrayNullToEmptyListProvider(PropertyInfo property)
        {
            Property = property;
        }

        /// <summary>
        /// </summary>
        public object GetValue(object target) => Property.GetValue(target) ?? new object[0];

        /// <summary>
        /// </summary>
        public void SetValue(object target, object value) => Property.SetValue(target, value);
    }
}