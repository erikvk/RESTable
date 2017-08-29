using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace RESTar.Serialization
{
    internal class NullToEmptyStringValueProvider : IValueProvider
    {
        private readonly PropertyInfo Property;

        public NullToEmptyStringValueProvider(PropertyInfo property)
        {
            Property = property;
        }

        public object GetValue(object target) => Property.GetValue(target) ?? "";
        public void SetValue(object target, object value) => Property.SetValue(target, value);
    }
}