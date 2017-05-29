using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace RESTar.View.Serializer
{
    public class ClassNullToEmptyObjectProvider : IValueProvider
    {
        private readonly PropertyInfo Property;

        public ClassNullToEmptyObjectProvider(PropertyInfo property)
        {
            Property = property;
        }

        public object GetValue(object target)
        {
            return Property.GetValue(target) ?? new object();
        }

        public void SetValue(object target, object value) => Property.SetValue(target, value);
    }
}