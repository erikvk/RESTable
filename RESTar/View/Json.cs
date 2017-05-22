using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RESTar.Operations;
using Starcounter;

namespace RESTar.View
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

    public class NullToEmptyStringValueProvider : IValueProvider
    {
        private readonly PropertyInfo Property;

        public NullToEmptyStringValueProvider(PropertyInfo property)
        {
            Property = property;
        }

        public object GetValue(object target) => Property.GetValue(target) ?? "";
        public void SetValue(object target, object value) => Property.SetValue(target, value);
    }

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

    public class ObjectIDProvider : IValueProvider
    {
        public object GetValue(object target) => Do.Try(target.GetObjectID, "");

        public void SetValue(object target, object value)
        {
        }
    }


    public class CreateViewModelResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            property.PropertyName = member.RESTarMemberName() + "$";
            return property;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var results = base.CreateProperties(type, memberSerialization);
            results.Add(new JsonProperty
            {
                PropertyType = typeof(string),
                PropertyName = "ObjectID$",
                Readable = true,
                Writable = false,
                ValueProvider = new ObjectIDProvider()
            });
            return results;
        }

        protected override IValueProvider CreateMemberValueProvider(MemberInfo member)
        {
            if (member.MemberType == MemberTypes.Property)
            {
                var pi = (PropertyInfo) member;
                if (pi.PropertyType.IsGenericType && pi.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return new NullableValueProvider(member, pi.PropertyType.GetGenericArguments().First());
                if (pi.PropertyType == typeof(string))
                    return new NullToEmptyStringValueProvider(pi);
                if (typeof(IEnumerable).IsAssignableFrom(pi.PropertyType))
                    return new ArrayNullToEmptyListProvider(pi);
                if (pi.PropertyType.IsClass)
                    return new ClassNullToEmptyObjectProvider(pi);
            }
            return base.CreateMemberValueProvider(member);
        }
    }

    public class CreateTemplateResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            property.PropertyName = member.RESTarMemberName();
            return property;
        }

        protected override IValueProvider CreateMemberValueProvider(MemberInfo member)
        {
            if (member.MemberType == MemberTypes.Property)
            {
                var pi = (PropertyInfo)member;
                if (pi.PropertyType.IsGenericType && pi.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return new NullableValueProvider(member, pi.PropertyType.GetGenericArguments().First());
                if (pi.PropertyType == typeof(string))
                    return new NullToEmptyStringValueProvider(pi);
                if (typeof(IEnumerable).IsAssignableFrom(pi.PropertyType))
                    return new ArrayNullToEmptyListProvider(pi);
                if (pi.PropertyType.IsClass)
                    return new ClassNullToEmptyObjectProvider(pi);
            }
            return base.CreateMemberValueProvider(member);
        }
    }
}