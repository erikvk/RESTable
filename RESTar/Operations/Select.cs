using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection;

namespace RESTar.Operations
{
    public class Select : List<PropertyChain>, ICollection<PropertyChain>, IProcessor
    {
        public IEnumerable<dynamic> Apply<T>(IEnumerable<T> entities)
        {
            return entities.Select(entity =>
            {
                var entityDict = entity as IDictionary<string, dynamic>;
                var dict = new Dictionary<string, dynamic>();
                ForEach(propToAdd =>
                {
                    var dictKey = entityDict?.MatchKeyIgnoreCase(propToAdd.Key);
                    if (!dict.ContainsKey(propToAdd.Key))
                        dict[dictKey ?? propToAdd.Key] = propToAdd.Get(entity);
                });
                return dict;
            });
        }

        private static readonly NoCaseComparer Comparer = new NoCaseComparer();
    }
}