using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;

namespace RESTar.Operations
{
    public class Add : List<PropertyChain>, IProcessor
    {
        public IEnumerable<dynamic> Apply<T>(IEnumerable<T> entities)
        {
            return entities.Select(entity =>
            {
                var dict = entity.MakeDictionary();
                ForEach(propToAdd =>
                {
                    if (!dict.ContainsKey(propToAdd.Key))
                        dict[propToAdd.Key] = propToAdd.Get(entity);
                });
                return dict;
            });
        }
    }
}