using System;
using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace RESTar.View.Serializer
{
    internal class RESTarMacroWrapperProvider : IValueProvider
    {
        private readonly PropertyInfo Property;

        public RESTarMacroWrapperProvider(PropertyInfo property)
        {
            Property = property;
        }

        public object GetValue(object target)
        {
            var val = Property.GetValue(target);
            var code = Type.GetTypeCode(val.GetType());
            switch (code)
            {
                case TypeCode.DateTime: return $"@RESTar(\"{(DateTime) Property.GetValue(target):O}\")";
                case TypeCode.String: return $"@RESTar(\"{Property.GetValue(target)}\")";
                default: return $"@RESTar({Property.GetValue(target)})";
            }
        }

        public void SetValue(object target, object value)
        {
        }
    }
}