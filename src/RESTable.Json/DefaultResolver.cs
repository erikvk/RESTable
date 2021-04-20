using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using RESTable.Meta;
using RESTable.Requests;

namespace RESTable.Json
{
    public class DefaultResolver : DefaultContractResolver
    {
        private JsonConverter StringEnumConverter { get; }
        private JsonConverter TypeConverter { get; }
        private JsonConverter AsyncEnumerableConverter { get; }
        private JsonConverter HeadersConverter { get; }
        private JsonConverter ContentTypeConverter { get; }
        private JsonConverter ContentTypesConverter { get; }
        private JsonConverter ToStringConverter { get; }
        private JsonConverter AggregatorTemplateConverter { get; }

        private TypeCache TypeCache { get; }

        public DefaultResolver(TypeCache typeCache)
        {
            StringEnumConverter = new StringEnumConverter();
            TypeConverter = new TypeConverter();
            AsyncEnumerableConverter = new AsyncEnumerableConverter();
            HeadersConverter = new HeadersConverter();
            ContentTypeConverter = new ContentTypeConverter();
            ContentTypesConverter = new ContentTypesConverter();
            ToStringConverter = new ToStringConverter();
            AggregatorTemplateConverter = new AggregatorTemplateConverter();

            TypeCache = typeCache;
            DateTimeConverters = new Dictionary<string, DateTimeConverter>();
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
            contract.Converter = objectType switch
            {
                _ when objectType.HasAttribute<JsonConverterAttribute>(out var attribute) => (JsonConverter) Activator.CreateInstance(attribute.ConverterType,
                    attribute.ConverterParameters),
                _ when objectType.IsSubclassOf(typeof(Type)) => TypeConverter,
                _ when objectType == typeof(Headers) => HeadersConverter,
                _ when objectType == typeof(ContentType) => ContentTypeConverter,
                _ when objectType == typeof(ContentTypes) => ContentTypesConverter,
                _ when objectType == typeof(Term) => ToStringConverter,
                _ when objectType == typeof(Aggregator) => AggregatorTemplateConverter,
                _ when objectType.IsEnum => StringEnumConverter,
                _ when objectType.ImplementsGenericInterface(typeof(IAsyncEnumerable<>)) => AsyncEnumerableConverter,
                _ => contract.Converter
            };
            if (entityTypeContract.CustomCreator != null)
                contract.DefaultCreator = () => entityTypeContract.CustomCreator();
            return contract;
        }

        internal IDictionary<string, DateTimeConverter> DateTimeConverters { get; }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            switch (member)
            {
                case PropertyInfo propertyInfo:
                    var property = TypeCache.GetDeclaredProperty(propertyInfo);
                    if (property == null || property.Hidden)
                        return null;
                    var p = base.CreateProperty(propertyInfo, memberSerialization);
                    if (property.IsDateTime)
                    {
                        var format = property.CustomDateTimeFormat ?? "O";
                        if (!DateTimeConverters.TryGetValue(format, out var converter))
                            converter = DateTimeConverters[format] = new DateTimeConverter(format);
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
            var declaredPropertiesCopy = TypeCache.GetDeclaredProperties(type).ToDictionary(p => p.Key, p => p.Value);
            foreach (var property in properties)
            {
                if (property.PropertyName is string name)
                    declaredPropertiesCopy.Remove(name);
            }
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