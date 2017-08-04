using System;
using System.Collections.Generic;
using RESTar.Deflection.Dynamic;
using RESTar.Linq;
using static RESTar.Operators;
using static RESTar.RESTarPresets;

namespace RESTar
{
    /// <summary>
    /// Gets a schema for a given resource
    /// </summary>
    [RESTar(ReadOnly, Singleton = true)]
    public class Schema : Dictionary<string, string>, ISelector<Schema>
    {
        /// <summary>
        /// The name of the resource to get the schema for
        /// </summary>
        public string Resource { private get; set; }

        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
        public IEnumerable<Schema> Select(IRequest<Schema> request)
        {
            var resourceName = request.Conditions.Get("resource", EQUALS)?.Value as string;
            if (resourceName == null)
                throw new Exception("Invalid syntax in request to RESTar.Schema. Format: " +
                                    "/schema/resource=insert_resource_name_here");
            var res = RESTar.Resource.Find(resourceName);
            if (res.IsDynamic) return null;
            var schema = new Schema();
            res.GetStaticProperties().Values.ForEach(p => schema[p.Name] = p.Type.FullName);
            return new[] {schema};
        }
    }
}