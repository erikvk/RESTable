using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Starcounter;
using RESTar.Deflection.Dynamic;

namespace RESTar.Serialization
{
    /// <inheritdoc />
    internal class CreateViewModelResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Property:
                    var property = member.GetStaticProperty();
                    if (property?.Hidden != false) return null;
                    var p = base.CreateProperty(member, memberSerialization);
                    p.PropertyName = property.Name;
                    p.Order = property.Order;
                    if (property.Writable)
                    {
                        p.PropertyName += "$";
                        p.Writable = true;
                    }
                    else p.Writable = false;
                    return p;
                case MemberTypes.Field:
                    if (member.RESTarIgnored()) return null;
                    var f = base.CreateProperty(member, memberSerialization);
                    f.PropertyName = member.RESTarMemberName();
                    if (f.Writable) f.PropertyName += "$";
                    return f;
                default: return null;
            }
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