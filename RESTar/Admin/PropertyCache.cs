using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Linq;

namespace RESTar.Admin
{
    /// <inheritdoc />
    /// <summary>
    /// Gets the properties discovered by this RESTar instance
    /// </summary>
    [RESTar(Methods.GET)]
    public class PropertyCache : ISelector<PropertyCache>
    {
        /// <summary>
        /// The type containing the properties
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// The discovered properties
        /// </summary>
        public IEnumerable<DeclaredProperty> Properties { get; private set; }

        /// <inheritdoc />
        public IEnumerable<PropertyCache> Select(IRequest<PropertyCache> request) => TypeCache
            .DeclaredPropertyCache
            .Select(item => new PropertyCache
            {
                Type = item.Key.RESTarTypeName(),
                Properties = item.Value.Values
            })
            .Where(request.Conditions);
    }
}