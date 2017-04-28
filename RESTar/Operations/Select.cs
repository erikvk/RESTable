using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;
using static System.StringComparison;

namespace RESTar.Operations
{
    public class Select : List<PropertyChain>, ICollection<PropertyChain>, IProcessor
    {
        public IEnumerable<dynamic> Apply<T>(IEnumerable<T> entities)
        {
            var customEntities = entities as IEnumerable<IDictionary<string, dynamic>>;
            if (customEntities != null)
                return customEntities.Select(entity =>
                {
                    entity.Keys
                        .Except(this.Select(pc => pc.Key), Comparer)
                        .ToList()
                        .ForEach(k => entity.Remove(k));
                    return entity;
                });
            return entities.Select(entity => this.ToDictionary(prop => prop.Key, prop => prop.GetValue(entity)));
        }

        private static readonly NoCaseComparer Comparer = new NoCaseComparer();

        private class NoCaseComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y) => string.Equals(x, y, CurrentCultureIgnoreCase);
            public int GetHashCode(string obj) => obj.ToLower().GetHashCode();
        }
    }
}