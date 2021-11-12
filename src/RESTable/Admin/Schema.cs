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

namespace RESTable.Admin
{
    /// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
    /// <summary>
    /// The Schema resource provides schemas for non-dynamic RESTable resources
    /// </summary>
    [RESTable(GET, Description = description)]
    public class Schema : Dictionary<string, object?>, ISelector<Schema>
    {
        private const string description = "The Schema resource provides schemas for " +
                                           "non-dynamic RESTable resources.";

        /// <summary>
        /// The name of the resource to get the schema for. Private get, so only for matching conditions,
        /// not visible in the resource entities.
        /// </summary>
        [RESTableParameter] public string? Resource { get; set; }

        public Schema(IEnumerable<KeyValuePair<string, object?>> collection)
        {
            foreach (var (key, value) in collection)
            {
                Add(key, value);
            }
        }

        /// <inheritdoc />
        public IEnumerable<Schema> Select(IRequest<Schema> request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (!request.Conditions.HasParameter("$" + nameof(Resource), out string? resourceName))
                throw new Exception("Invalid syntax in request to RESTable.Schema. Format: " +
                                    "/RESTable.Admin.Schema/$resource=<insert_resource_name_here>");
            var resource = request.GetRequiredService<ResourceCollection>().FindResource(resourceName) as IEntityResource;
            if (resource?.IsDynamic != false)
                yield break;
            yield return new Schema
            (
                collection: resource.Members.Values.Select(p => new KeyValuePair<string, object?>(p.Name, p.Type.FullName))
            );
        }
    }
}