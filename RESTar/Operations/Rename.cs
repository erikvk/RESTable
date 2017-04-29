using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;
using static System.StringSplitOptions;

namespace RESTar.Operations
{
    public class Rename : Dictionary<PropertyChain, string>, IProcessor
    {
        public static Rename Parse(string input, IResource resource)
        {
            var rename = new Rename();
            input.Split(',')
                .ForEach(str => rename.Add(PropertyChain.Parse(str.Split(new[] {"->"}, None)[0].ToLower(), resource),
                    str.Split(new[] {"->"}, None)[1]));
            return rename;
        }

        private IDictionary<string, dynamic> RenameDict(IDictionary<string, dynamic> entity)
        {
            this.ForEach(pair =>
            {
                string actualKey;
                var value = entity.SafeGetNoCase(pair.Key.Key, out actualKey);
                if (actualKey != null)
                    entity.Remove(actualKey);
                entity[pair.Value] = value;
            });
            return entity;
        }

        public IEnumerable<dynamic> Apply<T>(IEnumerable<T> entities)
        {
            return entities.Select(entity => RenameDict(entity.MakeDictionary()));
        }
    }
}