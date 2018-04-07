using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RESTar.Reflection.Dynamic;
using System.Linq;
using RESTar.Linq;

namespace RESTar.Serialization.NativeProtocol
{
    /// <inheritdoc />
    internal class CreateViewModelResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            switch (member)
            {
                case PropertyInfo propertyInfo:
                    var property = propertyInfo.GetDeclaredProperty();
                    if (property?.Hidden != false) return null;
                    var p = base.CreateProperty(member, memberSerialization);
                    p.PropertyName = property.Name;
                    p.ObjectCreationHandling = property.ReplaceOnUpdate ? ObjectCreationHandling.Replace : ObjectCreationHandling.Auto;
                    p.Order = property.Order;
                    if (property.Writable)
                    {
                        p.PropertyName += "$";
                        p.Writable = true;
                    }
                    else p.Writable = false;
                    return p;
                case FieldInfo fieldInfo:
                    if (fieldInfo.RESTarIgnored()) return null;
                    var f = base.CreateProperty(fieldInfo, memberSerialization);
                    f.PropertyName = fieldInfo.RESTarMemberName();
                    if (f.Writable) f.PropertyName += "$";
                    return f;
                default: return null;
            }
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);
            type.GetDeclaredProperties()
                .Values
                .OfType<SpecialProperty>()
                .Where(p => !p.Hidden)
                .Select(p => p.JsonProperty)
                .ForEach(properties.Add);
            return properties;
        }

        protected override IValueProvider CreateMemberValueProvider(MemberInfo member)
        {
            if (member.MemberType == MemberTypes.Property)
            {
                var pi = (PropertyInfo) member;
                if (pi.PropertyType.IsGenericType && pi.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return new NullableValueProvider(member, pi.PropertyType.GetGenericArguments()[0]);
                if (pi.PropertyType == typeof(string))
                    return new NullToEmptyStringValueProvider(pi);
                if (typeof(IEnumerable).IsAssignableFrom(pi.PropertyType))
                    return new ArrayNullToEmptyListProvider(pi);
                if (pi.PropertyType == typeof(object))
                    return new RESTarMacroWrapperProvider(pi);
                if (pi.PropertyType.IsClass)
                    return new ClassNullToEmptyObjectProvider(pi);
            }
            return base.CreateMemberValueProvider(member);
        }
    }
}