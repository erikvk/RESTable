using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dynamit;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using RESTar.Internal.Sc;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Meta.Internal;

namespace RESTar.ContentTypeProviders.NativeJsonProtocol
{
    internal class DefaultResolver : DefaultContractResolver
    {
        private static readonly JsonConverter DDictionaryConverter;
        private static readonly JsonConverter StringEnumConverter;

        static DefaultResolver()
        {
            DDictionaryConverter = new DDictionaryConverter();
            StringEnumConverter = new StringEnumConverter();
        }

        protected override string ResolveDictionaryKey(string dictionaryKey)
        {
            var g = base.ResolveDictionaryKey(dictionaryKey);
            return g;
        }

        protected override JsonContract CreateContract(Type objectType)
        {
            var contract = base.CreateContract(objectType);
            switch (objectType)
            {
                case var _ when typeof(DDictionary).IsAssignableFrom(objectType) && contract is JsonDictionaryContract jc:
                    jc.Converter = DDictionaryConverter;
                    jc.ItemIsReference = true;
                    break;
                case var _ when objectType.IsEnum:
                    contract.Converter = StringEnumConverter;
                    break;
            }
            return contract;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            switch (member)
            {
                case PropertyInfo propertyInfo:
                    var property = propertyInfo.GetDeclaredProperty();
                    if (property == null || property.Hidden)
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
            type.GetDeclaredProperties()
                .Values
                .OfType<SpecialProperty>()
                .Where(p => !p.Hidden)
                .Select(p => p.JsonProperty)
                .ForEach(properties.Add);
            return properties;
        }
    }
}