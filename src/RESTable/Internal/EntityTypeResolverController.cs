using RESTable.Meta;
using RESTable.Resources;

namespace RESTable.Internal
{
    internal static class EntityTypeResolverController
    {
        internal static void SetupEntityTypeResolvers(IEntityTypeContractResolver[] resolvers)
        {
            EntityTypeResolvers = resolvers ?? new IEntityTypeContractResolver[0];
        }

        internal static IEntityTypeContractResolver[] EntityTypeResolvers { get; private set; }

        internal static void InvokeContractResolvers(EntityTypeContract contract)
        {
            foreach (var resolver in EntityTypeResolvers)
            {
                resolver.ResolveContract(contract);
            }
        }
    }
}