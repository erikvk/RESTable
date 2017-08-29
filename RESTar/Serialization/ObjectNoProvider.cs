using Newtonsoft.Json.Serialization;
using RESTar.Operations;
using Starcounter;

namespace RESTar.Serialization
{
    internal class ObjectNoProvider : IValueProvider
    {
        public object GetValue(object target) => Do.Try(target.GetObjectNo, 0UL);

        public void SetValue(object target, object value)
        {
        }
    }
}