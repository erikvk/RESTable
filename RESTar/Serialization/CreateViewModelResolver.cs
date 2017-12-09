using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Starcounter;

namespace RESTar.Serialization
{
    /// <inheritdoc />
    internal class CreateViewModelResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            if (member.ShouldBeIgnored() || member.ShouldBeHidden()) return null;
            var property = base.CreateProperty(member, memberSerialization);
            if (member.ShouldBeReadOnly()) property.Writable = false;
            if (property.Writable)
                property.PropertyName = member.RESTarMemberName() + "$";
            else property.PropertyName = member.RESTarMemberName();
            return property;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var results = base.CreateProperties(type, memberSerialization);
            if (type.HasAttribute<DatabaseAttribute>())
            {
                results.Add(new JsonProperty
                {
                    PropertyType = typeof(ulong),
                    PropertyName = "ObjectNo",
                    Readable = true,
                    Writable = false,
                    ValueProvider = new ObjectNoProvider()
                });
                results.Add(new JsonProperty
                {
                    PropertyType = typeof(string),
                    PropertyName = "ObjectID",
                    Readable = true,
                    Writable = false,
                    ValueProvider = new ObjectIDProvider()
                });
            }
            return results;
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