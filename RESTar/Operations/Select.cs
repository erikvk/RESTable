using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Serialization;

namespace RESTar.Operations
{
    internal class Select : List<Term>, ICollection<Term>, IProcessor
    {
        internal Select(IResource resource, string keys, ICollection<string> dynDomain) => keys
            .Split(',')
            .Distinct()
            .Select(key => resource.MakeOutputTerm(key, dynDomain))
            .ForEach(Add);

        public IEnumerable<JObject> Apply<T>(IEnumerable<T> entities) => entities.Select(entity =>
        {
            var jobj = new JObject();
            ForEach(term =>
            {
                if (jobj[term.Key] != null) return;
                object val = term.Evaluate(entity, out var actualKey);
                jobj[actualKey] = val?.ToJToken();
            });
            return jobj;
        });
    }
}