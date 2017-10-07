using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using static RESTar.Methods;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Admin
{
    /// <summary>
    /// The TableInfo resource can create an aggregated info view for a given Starcounter table.
    /// </summary>
    [RESTar(GET, Description = description)]
    public class ResourceProfiler : ResourceProfile, ISelector<ResourceProfiler>
    {
        private const string description = "The TableInfo resource can create aggregated " +
                                           "info views for Starcounter tables.";

        private ResourceProfiler(Type type)
        {
            var profile = Make(type);
            ResourceName = profile.ResourceName;
            NumberOfEntities = profile.NumberOfEntities;
            ApproximateSize = profile.ApproximateSize;
        }

        /// <inheritdoc />
        public IEnumerable<ResourceProfiler> Select(IRequest<ResourceProfiler> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            IEnumerable<IResource> resources;
            string input = request.Conditions.Get(nameof(ResourceName), Operators.EQUALS)?.Value;
            if (input == null)
                resources = RESTarConfig.Resources.Where(r => r.IsStarcounterResource);
            else
            {
                var resource = RESTar.Resource.Find(input);
                if (!resource.IsStarcounterResource)
                    throw new Exception($"'{resource.Name}' is not a Starcounter resource, and has no table info");
                resources = new[] {resource};
            }
            return resources
                .Select(r => new ResourceProfiler(r.Type))
                .Where(request.Conditions)
                .OrderByDescending(t => t.ApproximateSize.Bytes)
                .ToList();
        }
    }
}