using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RESTar.Deflection.Dynamic;
using static Newtonsoft.Json.NullValueHandling;
using static Newtonsoft.Json.ObjectCreationHandling;

namespace RESTar.Serialization
{
    internal class DefaultResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Property:
                    var property = member.GetDeclaredProperty();
                    if (property?.Hidden != false) return null;
                    var p = base.CreateProperty(member, memberSerialization);
                    p.Writable = property.Writable;
                    p.NullValueHandling = property.HiddenIfNull ? Ignore : Include;
                    p.ObjectCreationHandling = property.ReplaceOnUpdate ? Replace : Auto;
                    p.PropertyName = property.Name;
                    p.Order = property.Order;
                    return p;
                case MemberTypes.Field:
                    if (member.RESTarIgnored()) return null;
                    var f = base.CreateProperty(member, memberSerialization);
                    f.PropertyName = member.RESTarMemberName();
                    return f;
                default: return null;
            }
        }
    }
}