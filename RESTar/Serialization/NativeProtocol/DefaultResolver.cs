using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dynamit;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RESTar.Deflection.Dynamic;

namespace RESTar.Serialization.NativeProtocol
{
    internal class DefaultResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            switch (member)
            {
                case PropertyInfo propertyInfo:
                    var property = propertyInfo.GetDeclaredProperty();
                    if (property == null || !property.IsKey && property.Hidden)
                        return null;
                    var p = base.CreateProperty(propertyInfo, memberSerialization);
                    p.Writable = property.Writable;
                    p.NullValueHandling = property.HiddenIfNull ? NullValueHandling.Ignore : NullValueHandling.Include;
                    p.ObjectCreationHandling = property.ReplaceOnUpdate ? ObjectCreationHandling.Replace : ObjectCreationHandling.Auto;
                    p.PropertyName = property.Name;
                    p.Order = property.Order;
                    return p;
                case FieldInfo fieldInfo:
                    if (fieldInfo.RESTarIgnored()) return null;
                    var f = base.CreateProperty(fieldInfo, memberSerialization);
                    f.PropertyName = fieldInfo.RESTarMemberName();
                    return f;
                default: return null;
            }
        }
        
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);

            if (typeof(DDictionary).IsAssignableFrom(type))
            {
                var s = "";
            }

            foreach (var specialProperty in type.GetDeclaredProperties().Values.OfType<SpecialProperty>())
            {
                if (specialProperty.IsKey || !specialProperty.Hidden)
                    properties.Add(specialProperty.JsonProperty);
            }
            return properties;
        }
    }
}