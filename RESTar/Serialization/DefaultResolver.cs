using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RESTar.Deflection.Dynamic;
using static Newtonsoft.Json.NullValueHandling;

namespace RESTar.Serialization
{
    internal class DefaultResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = member.GetStaticProperty();
            if (property?.Hidden != false) return null;
            var p = base.CreateProperty(member, memberSerialization);
            p.Writable = property.Writable;
            p.NullValueHandling = property.HiddenIfNull ? Ignore : Include;
            p.PropertyName = property.Name;
            p.Order = property.Order;
            return p;
        }
    }
}