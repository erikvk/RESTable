using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using RESTar.Linq;
using static RESTar.Methods;
using static RESTar.Operators;

namespace RESTar.Admin
{
    /// <summary>
    /// The Schema resource provides schemas for non-dynamic RESTar resources
    /// </summary>
    [RESTar(GET, Singleton = true, Description = description)]
    internal class Schema : JObject, ISelector<Schema>
    {
        private const string description = "The Schema resource provides schemas for " +
                                           "non-dynamic RESTar resources.";

        /// <summary>
        /// The name of the resource to get the schema for
        /// </summary>
        public string Resource { private get; set; }

        /// <inheritdoc />
        public IEnumerable<Schema> Select(IRequest<Schema> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (!(request.Conditions.Get("resource", EQUALS)?.Value is string resourceName))
                throw new Exception("Invalid syntax in request to RESTar.Schema. Format: " +
                                    "/schema/resource=insert_resource_name_here");
            var res = RESTar.Resource.Find(resourceName) as IEntityResource;
            if (res?.IsDynamic != false) return null;
            var schema = new Schema();
            res.Members.Values.ForEach(p => schema[p.Name] = p.Type.FullName);
            return new[] {schema};
        }
    }
}