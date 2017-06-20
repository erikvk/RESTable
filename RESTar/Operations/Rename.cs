using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Deflection;
using RESTar.Internal;
using static System.StringSplitOptions;

namespace RESTar.Operations
{
    public class Rename : Dictionary<PropertyChain, string>, IProcessor
    {
        public static Rename Parse(string input, IResource resource)
        {
            var opMatcher = input.Contains("->") ? "->" : "-%3E";
            var rename = new Rename();
            input.Split(',')
                .ForEach(str => rename.Add(
                    PropertyChain.Parse(str.Split(new[] {opMatcher}, None)[0].ToLower(), resource, resource.IsDynamic),
                    str.Split(new[] {opMatcher}, None)[1]));
            return rename;
        }

        private JObject RenameJObject(JObject entity)
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
            return entities.Select(entity => RenameJObject(entity.MakeJObject()));
        }
    }
}