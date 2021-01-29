using RESTable.Meta;

namespace RESTable.Resources
{
    /// <summary>
    /// Represents the operations of resolving an entity type contract
    /// </summary>
    public interface IEntityTypeContractResolver
    {
        /// <summary>
        /// Lets the entity resource provider inspect encountered properties of a type and, for example, add additional properties
        /// </summary>
        public void ResolveContract(EntityTypeContract contract);
    }
}