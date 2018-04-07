using Newtonsoft.Json.Serialization;
using RESTar.Reflection.Dynamic;

namespace RESTar.Serialization
{
    /// <inheritdoc />
    /// <summary>
    /// A JSON.net IValueProvider that gets and sets using Deflection getters and setters
    /// </summary>
    internal class DefaultValueProvider : IValueProvider
    {
        private Getter Get { get; }
        private Setter Set { get; }
        public DefaultValueProvider(Property property) => (Get, Set) = (property.Getter, property.Setter);
        public void SetValue(object target, object value) => Set(target, value);
        public object GetValue(object target) => Get(target);
    }
}