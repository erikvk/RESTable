using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using RESTable.Meta;

namespace RESTable.ContentTypeProviders.NativeJsonProtocol
{
    internal class DefaultResolver : DefaultContractResolver
    {
        private static readonly JsonConverter StringEnumConverter;
        private static readonly JsonConverter TypeConverter;

        static DefaultResolver()
        {
            StringEnumConverter = new StringEnumConverter();
            TypeConverter = new TypeConverter();
        }

        protected override string ResolveDictionaryKey(string dictionaryKey)
        {
            var g = base.ResolveDictionaryKey(dictionaryKey);
            return g;
        }

        protected override JsonContract CreateContract(Type objectType)
        {
            var contract = base.CreateContract(objectType);
            var entityTypeContract = TypeCache.GetEntityTypeContract(objectType);
            switch (objectType)
            {
                case var _ when objectType.HasAttribute<JsonConverterAttribute>(out var attribute):
                    contract.Converter = (JsonConverter) Activator.CreateInstance(attribute.ConverterType, attribute.ConverterParameters);
                    break;
                case var _ when objectType.IsSubclassOf(typeof(Type)):
                    contract.Converter = TypeConverter;
                    break;
                case var _ when objectType.IsEnum:
                    contract.Converter = StringEnumConverter;
                    break;
            }
            if (entityTypeContract.CustomCreator != null)
                contract.DefaultCreator = () => entityTypeContract.CustomCreator();
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
                    if (property.IsDateTime)
                    {
                        var format = property.CustomDateTimeFormat ?? "O";
                        if (!DateTimeConverter.Converters.TryGetValue(format, out var converter))
                            converter = DateTimeConverter.Converters[format] = new DateTimeConverter(format);
                        p.Converter = converter;
                    }
                    p.Writable = property.IsWritable;
                    p.NullValueHandling = property.HiddenIfNull ? NullValueHandling.Ignore : NullValueHandling.Include;
                    p.ObjectCreationHandling = property.ReplaceOnUpdate ? ObjectCreationHandling.Replace : ObjectCreationHandling.Auto;
                    p.PropertyName = property.Name;
                    p.Order = property.Order;
                    p.ValueProvider = new DefaultValueProvider(property);
                    return p;
                case FieldInfo fieldInfo:
                    if (fieldInfo.RESTableIgnored()) return null;
                    var f = base.CreateProperty(fieldInfo, memberSerialization);
                    f.PropertyName = fieldInfo.RESTableMemberName();
                    return f;
                default: return null;
            }
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);
            var declaredPropertiesCopy = type.GetDeclaredProperties().ToDictionary(p => p.Key, p => p.Value);
            foreach (var property in properties)
                declaredPropertiesCopy.Remove(property.PropertyName);
            var additionalProperties = declaredPropertiesCopy
                .Values
                .Where(p => !p.Hidden)
                .Select(p => new JsonProperty
                {
                    PropertyType = p.Type,
                    PropertyName = p.Name,
                    Readable = p.IsReadable,
                    Writable = p.IsWritable,
                    ValueProvider = new DefaultValueProvider(p),
                    ObjectCreationHandling = p.ReplaceOnUpdate ? ObjectCreationHandling.Replace : ObjectCreationHandling.Reuse,
                    NullValueHandling = p.HiddenIfNull ? NullValueHandling.Ignore : NullValueHandling.Include,
                    Order = p.Order
                });
            return properties.Union(additionalProperties).ToList();
        }
    }
}