using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
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
    /// <summary>
    /// The Schema resource provides schemas for non-dynamic RESTable resources
    /// </summary>
    [RESTable(GET, Description = description)]
    internal class Schema : Dictionary<string, object>, ISelector<Schema>
    {
        private const string description = "The Schema resource provides schemas for " +
                                           "non-dynamic RESTable resources.";

        /// <summary>
        /// The name of the resource to get the schema for
        /// </summary>
        public string Resource { private get; set; }

        public Schema(IEnumerable<KeyValuePair<string, object>> collection)
        {
            foreach (var (key, value) in collection)
            {
                Add(key, value);
            }
        }

        /// <inheritdoc />
        public IEnumerable<Schema> Select(IRequest<Schema> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var resourceCondition = request.Conditions.Pop(nameof(Resource), EQUALS);
            if (!(resourceCondition?.Value is string resourceName))
                throw new Exception("Invalid syntax in request to RESTable.Schema. Format: " +
                                    "/RESTable.Admin.Schema/resource=<insert_resource_name_here>");
            var resource = request.GetService<ResourceCollection>().FindResource(resourceName) as IEntityResource;
            if (resource?.IsDynamic != false)
                yield break;
            yield return new Schema
            (
                collection: resource.Members.Values.Select(p => new KeyValuePair<string, object>(p.Name, p.Type.FullName))
            );
        }
    }
}