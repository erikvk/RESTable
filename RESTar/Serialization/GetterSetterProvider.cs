using Newtonsoft.Json.Serialization;
using RESTar.Deflection.Dynamic;

namespace RESTar.Serialization
{
    /// <inheritdoc />
    /// <summary>
    /// A JSON.net IValueProvider that gets and sets using Deflection getters and setters
    /// </summary>
    internal class GetterSetterProvider : IValueProvider
    {
        private Getter Get { get; }
        private Setter Set { get; }
        public GetterSetterProvider(Getter getter, Setter setter) => (Get, Set) = (getter, setter);
        public void SetValue(object target, object value) => Set(target, value);
        public object GetValue(object target) => Get(target);
    }
}