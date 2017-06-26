using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Deflection;

namespace RESTar.Operations
{
    public class Add : List<PropertyChain>, IProcessor
    {
        public IEnumerable<JObject> Apply<T>(IEnumerable<T> entities)
        {
            return entities.Select(entity =>
            {
                var jobj = entity.ToJObject();
                ForEach(prop =>
                {
                    if (jobj[prop.Key] == null)
                    {
                        var val = prop.Get(entity, out string actualKey);
                        jobj[actualKey] = val == null ? null : JToken.FromObject(val, Serializer.JsonSerializer);
                    }
                });
                return jobj;
            });
        }
    }
}