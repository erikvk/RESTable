using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RESTar.View.Serializer
{
    public class DefaultResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            if (member.HasAttribute<IgnoreDataMemberAttribute>())
                return null;
            var property = base.CreateProperty(member, memberSerialization);
            property.PropertyName = member.MemberName();
            return property;
        }
    }
}