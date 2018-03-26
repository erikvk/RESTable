using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Resources;
using static RESTar.Method;
using static RESTar.Operators;

namespace RESTar.Admin
{
    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="JObject" />
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
        public IEnumerable<Schema> Select(IQuery<Schema> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (!(query.Conditions.Get("resource", EQUALS)?.Value is string resourceName))
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