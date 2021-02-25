using Newtonsoft.Json.Serialization;
using RESTable.Meta;

namespace RESTable.Json
{
    /// <inheritdoc />
    /// <summary>
    /// A JSON.net IValueProvider that gets and sets using Deflection getters and setters
    /// </summary>
    public class DefaultValueProvider : IValueProvider
    {
        private readonly Property Property;
        public DefaultValueProvider(Property property) => Property = property;
        public object GetValue(object target) => Property.GetValue(target);
        public void SetValue(object target, object value) => Property.SetValue(target, value);
    }
}