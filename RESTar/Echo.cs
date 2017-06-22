using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace RESTar
{
    [RESTar(RESTarPresets.ReadOnly, Dynamic = true, AllowDynamicConditions = true)]
    public class Echo : JObject, ISelector<Echo>
    {
        public Echo()
        {
        }

        private Echo(object thing) : base(thing)
        {
        }

        public IEnumerable<Echo> Select(IRequest request) => request.Conditions == null
            ? new[] {new Echo()}
            : new[] {new Echo(request.Conditions.Select(c => new JProperty(c.Key, c.Value)))};
    }
}