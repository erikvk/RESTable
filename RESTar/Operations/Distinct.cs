using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace RESTar.Operations
{
    /// <summary>
    /// Applies a distinct filtering to the inputted entities
    /// </summary>
    public class Distinct : IProcessor
    {
        /// <summary>
        /// Applies the distinct filtering
        /// </summary>
        public IEnumerable<JObject> Apply<T>(IEnumerable<T> entities) => entities
            .Select(entity => entity.ToJObject())
            .Distinct(JToken.EqualityComparer)
            .Cast<JObject>();
    }
}