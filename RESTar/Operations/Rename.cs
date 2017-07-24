using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using RESTar.Linq;
using static System.StringSplitOptions;

namespace RESTar.Operations
{
    internal class Rename : Dictionary<Term, string>, IProcessor
    {
        internal Rename(IResource resource, string key, out IEnumerable<string> dynamicDomain)
        {
            var opMatcher = key.Contains("->") ? new[] {"->"} : new[] {"-%3E"};
            key.Split(',').ForEach(str => Add(
                key: Term.ParseInternal(resource.Type, str.Split(opMatcher, None)[0].ToLower(), resource.IsDynamic),
                value: str.Split(opMatcher, None)[1])
            );
            dynamicDomain = Values;
        }

        private JObject Renamed(JObject entity)
        {
            this.ForEach(pair =>
            {
                var value = entity.SafeGetNoCase(pair.Key.Key, out string actualKey);
                if (actualKey != null)
                    entity.Remove(actualKey);
                entity[pair.Value] = value;
            });
            return entity;
        }

        public IEnumerable<JObject> Apply<T>(IEnumerable<T> entities)
        {
            return entities.Select(entity => Renamed(entity.ToJObject()));
        }
    }
}