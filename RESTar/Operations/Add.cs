using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Deflection;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using RESTar.Linq;

namespace RESTar.Operations
{
    internal class Add : List<Term>, IProcessor
    {
        internal Add(IResource resource, string key, IEnumerable<string> dynDomain) => key
            .ToLower()
            .Split(',')
            .Distinct()
            .If(dynDomain == null,
                then: s => s.Select(_s => resource.MakeTerm(_s, resource.IsDynamic)),
                @else: s => s.Select(_s => Term.ParseInternal(resource.Type, _s, resource.IsDynamic, dynDomain)))
            .ForEach(Add);

        public IEnumerable<JObject> Apply<T>(IEnumerable<T> entities) => entities.Select(entity =>
        {
            var keyMatcher = typeof(T).Implements(typeof(IDictionary<,>)) ? typeof(T).MakeKeyMatcher() : null;
            var jobj = entity.ToJObject();
            ForEach(term =>
            {
                if (jobj[term.Key] != null) return;
                object val = term.Evaluate(entity, out string actualKey);
                jobj[keyMatcher?.Invoke(entity, term.Key) ?? actualKey] = val?.ToJToken();
            });
            return jobj;
        });
    }
}