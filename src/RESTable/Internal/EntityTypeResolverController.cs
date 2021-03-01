using System.Collections.Generic;
using System.Linq;
using RESTable.Meta;
using RESTable.Resources;

namespace RESTable.Internal
{
    public class EntityTypeResolverController
    {
        internal IEntityTypeContractResolver[] EntityTypeResolvers { get; }

        public EntityTypeResolverController(IEnumerable<IEntityTypeContractResolver> entityTypeResolvers)
        {
            EntityTypeResolvers = entityTypeResolvers.ToArray();
        }

        internal void InvokeContractResolvers(EntityTypeContract contract)
        {
            foreach (var resolver in EntityTypeResolvers)
            {
                resolver.ResolveContract(contract);
            }
        }
    }
}