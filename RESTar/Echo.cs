using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using static RESTar.RESTarPresets;

namespace RESTar
{
    /// <summary>
    /// The echo resource is a test resource that returns the conditions
    /// inputted as a JSON object.
    /// </summary>
    [RESTar(ReadOnly, AllowDynamicConditions = true)]
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
        public IEnumerable<Echo> Select(IRequest<Echo> request) => !request.Conditions.Any
            ? new[] {new Echo()}
            : new[] {new Echo(request.Conditions.Select(c => new JProperty(c.Key, c.Value)))};
    }
}