using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;

namespace RESTar.Operations
{
    public class Rename : Dictionary<PropertyChain, string>, IProcessor
    {
        public static Rename Parse(string input, IResource resource)
        {
            var rename = new Rename();
            input.Split(',')
                .ForEach(str => rename.Add(
                    PropertyChain.Parse(str.Split(new[] {"->"}, StringSplitOptions.None)[0].ToLower(), resource),
                    str.Split(new[] {"->"}, StringSplitOptions.None)[1]));
            return rename;
        }

        private IDictionary<string, dynamic> RenameDict(IDictionary<string, dynamic> entity)
        {
            this.ForEach(pair =>
            {
                string actualKey;
                if (!entity.ContainsKeyIgnorecase(pair.Key.Key, out actualKey))
                    return;
                var value = entity[actualKey];
                entity.Remove(actualKey);
                entity[pair.Value] = value;
            });
            return entity;
        }

        public IEnumerable<dynamic> Apply<T>(IEnumerable<T> entities)
        {
            var customEntities = entities as IEnumerable<IDictionary<string, dynamic>>;
            return customEntities?.Select(RenameDict) ?? entities.Select(entity => RenameDict(entity.MakeDictionary()));
        }
    }
}