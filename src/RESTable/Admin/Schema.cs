using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.Linq;
using static RESTable.Method;
using static RESTable.Requests.Operators;

namespace RESTable.Admin
{
    /// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
    /// <inheritdoc cref="JObject" />
    /// <summary>
    /// The Schema resource provides schemas for non-dynamic RESTable resources
    /// </summary>
    [RESTable(GET, Description = description)]
    internal class Schema : JObject, ISelector<Schema>
    {
        private const string description = "The Schema resource provides schemas for " +
                                           "non-dynamic RESTable resources.";

        /// <summary>
        /// The name of the resource to get the schema for
        /// </summary>
        public string Resource { private get; set; }

        /// <inheritdoc />
        public IEnumerable<Schema> Select(IRequest<Schema> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var resourceCondition = request.Conditions.Pop("resource", EQUALS);
            if (!(resourceCondition?.Value is string resourceName))
                throw new Exception("Invalid syntax in request to RESTable.Schema. Format: " +
                                    "/schema/resource=insert_resource_name_here");
            var res = Meta.Resource.Find(resourceName) as IEntityResource;
            if (res?.IsDynamic != false)
                yield break;
            var schema = new Schema();
            res.Members.Values.ForEach(p => schema[p.Name] = p.Type.FullName);
            yield return schema;
        }
    }
}