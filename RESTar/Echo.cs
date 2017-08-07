using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using static RESTar.Methods;

namespace RESTar
{
    /// <summary>
    /// The echo resource is a test resource that returns the conditions
    /// inputted as a JSON object.
    /// </summary>
    [RESTar(GET, AllowDynamicConditions = true)]
    public class Echo : JObject, ISelector<Echo>
    {
        private Echo()
        {
        }

        private Echo(object thing) : base(thing)
        {
        }

        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
        public IEnumerable<Echo> Select(IRequest<Echo> request)
        {
            var echo = new[]
            {
                new Echo(request.Conditions.Select(c => new JProperty(c.Key, c.Value)))
            };
            Deflection.Dynamic.TypeCache.ClearTermsFor<Echo>();
            return echo;
        }
    }
}