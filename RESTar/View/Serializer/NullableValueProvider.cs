using System;
using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace RESTar.View.Serializer
{
    public class NullableValueProvider : IValueProvider
    {
        private readonly object DefaultValue;
        private readonly IValueProvider UnderlyingValueProvider;

        public NullableValueProvider(MemberInfo memberInfo, Type underlyingType)
        {
            UnderlyingValueProvider = new DynamicValueProvider(memberInfo);
            DefaultValue = Activator.CreateInstance(underlyingType);
        }

        public void SetValue(object target, object value) => UnderlyingValueProvider.SetValue(target, value);
        public object GetValue(object target) => UnderlyingValueProvider.GetValue(target) ?? DefaultValue;
    }
}