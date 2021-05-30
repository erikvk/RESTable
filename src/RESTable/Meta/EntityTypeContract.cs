using System;
using System.Collections.Generic;

namespace RESTable.Meta
{
    /// <summary>
    /// A type contract that records the members of an entity type, along with
    /// the method of obtaining an instance of it.
    /// </summary>
    public class EntityTypeContract
    {
        /// <summary>
        /// The entity type
        /// </summary>
        public Type EntityType { get; }

        /// <summary>
        /// The properties of the entity type
        /// </summary>
        public List<DeclaredProperty> Properties { get; }

        /// <summary>
        /// The function for obtaining an instance of this entity type
        /// </summary>
        public Constructor? CustomCreator { get; set; }

        public EntityTypeContract(Type entityType, List<DeclaredProperty> properties)
        {
            EntityType = entityType;
            Properties = properties;
        }
    }
}