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

#pragma warning disable 1591

namespace RESTable.Admin
{
    /// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
    /// <inheritdoc cref="IAsyncDeleter{T}" />
    /// <summary>
    /// The TermCache resource contains all the terms that RESTable has encountered 
    /// for a given resource type, for example in conditions.
    /// </summary>
    [RESTable(GET, DELETE, Description = description)]
    public class TermCache : ISelector<TermCache>, IDeleter<TermCache>
    {
        private const string description = "The TermCache resource contains all the terms that RESTable " +
                                           "has encountered for a given resource, for example in conditions.";

        public string Type { get; set; }
        public string[] Terms { get; set; }

        public IEnumerable<TermCache> Select(IRequest<TermCache> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var termCache = request.GetService<Requests.TermCache>();
            var resourceCollection = request.GetService<ResourceCollection>();
            return resourceCollection.Select(r => new TermCache
            {
                Type = r.Type.GetRESTableTypeName(),
                Terms = termCache
                    .Where(pair => pair.Key.Type == r.Type.GetRESTableTypeName())
                    .Select(pair => pair.Value.Key)
                    .ToArray()
            });
        }

        public int Delete(IRequest<TermCache> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var termCache = request.GetService<Requests.TermCache>();
            var count = 0;
            request.GetInputEntities().ForEach(e =>
            {
                termCache
                    .Where(pair => pair.Key.Type == e.Type)
                    .Select(pair => pair.Key).ToList()
                    .ForEach(key => termCache.Remove(key));
                count += 1;
            });
            return count;
        }
    }
}