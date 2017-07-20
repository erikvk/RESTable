using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Deflection;

namespace RESTar.Operations
{
    internal class Select : List<Term>, ICollection<Term>, IProcessor
    {
        public IEnumerable<JObject> Apply<T>(IEnumerable<T> entities)
        {
            return entities.Select(entity =>
            {
                var entityDict = entity as IDictionary<string, dynamic>;
                var entityJobj = entity as JObject;
                var jobj = new JObject();
                ForEach(prop =>
                {
                    var dictKey = entityDict?.MatchKeyIgnoreCase(prop.Key) ??
                                  entityJobj?.MatchKeyIgnoreCase(prop.Key);
                    if (jobj[prop.Key] == null)
                    {
                        var val = prop.Evaluate(entity, out string actualKey);
                        jobj[dictKey ?? actualKey] = val == null ? null : JToken.FromObject(val, Serializer.JsonSerializer);
                    }
                });
                return jobj;
            });
        }

        private static readonly NoCaseComparer Comparer = new NoCaseComparer();
    }
}