using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RESTar.Serialization
{
    internal class DefaultResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            if (member.ShouldBeIgnored() || member.ShouldBeHidden()) return null;
            var property = base.CreateProperty(member, memberSerialization);
            if (member.ShouldBeReadOnly()) property.Writable = false;
            if (member.ShouldBeHiddenIfNull()) property.NullValueHandling = NullValueHandling.Ignore;
            property.PropertyName = member.RESTarMemberName();
            return property;
        }
    }
}