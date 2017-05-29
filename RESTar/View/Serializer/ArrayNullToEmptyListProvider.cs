using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace RESTar.View.Serializer
{
    public class ArrayNullToEmptyListProvider : IValueProvider
    {
        private readonly PropertyInfo Property;

        public ArrayNullToEmptyListProvider(PropertyInfo property)
        {
            Property = property;
        }

        public object GetValue(object target) => Property.GetValue(target) ?? new object[0];
        public void SetValue(object target, object value) => Property.SetValue(target, value);
    }
}