using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Admin
{
    /// <inheritdoc />
    /// <summary>
    /// Gets the properties discovered by this RESTable instance
    /// </summary>
    [RESTable(Method.GET, Description = description)]
    public class PropertyCache : ISelector<PropertyCache>
    {
        private const string description = "Contains the types and properties discovered by RESTable when " +
                                           "working with the resources of the current RESTable application";

        /// <summary>
        /// The type containing the properties
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// The discovered properties
        /// </summary>
        public IEnumerable<DeclaredProperty> Properties { get; private set; }

        /// <inheritdoc />
        public IEnumerable<PropertyCache> Select(IRequest<PropertyCache> request) => TypeCache
            .DeclaredPropertyCache
            .Select(item => new PropertyCache
            {
                Type = item.Key,
                Properties = item.Value.Values
            });
    }
}