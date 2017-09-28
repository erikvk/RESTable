using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RESTar.Serialization
{
    internal class DefaultResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            if (member.HasAttribute<IgnoreDataMemberAttribute>())
                return null;
            var property = base.CreateProperty(member, memberSerialization);
            if (member.HasAttribute<ReadOnlyAttribute>())
                property.Writable = false;
            property.PropertyName = member.RESTarMemberName();
            return property;
        }
    }
}