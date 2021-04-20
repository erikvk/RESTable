using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using static RESTable.Method;

#pragma warning disable 1591

namespace RESTable.Admin
{
    /// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
    /// <inheritdoc cref="IDeleter{T}" />
    /// <summary>
    /// The TermCache resource contains all the terms that RESTable has encountered 
    /// for a given resource type, for example in conditions.
    /// </summary>
    [RESTable(GET, DELETE, Description = description)]
    public class TermCache : ISelector<TermCache>, IDeleter<TermCache>
    {
        private const string description = "The TermCache resource contains all the terms that RESTable " +
                                           "has encountered for a given resource, for example in conditions.";

        public string Type { get; }
        public string[] Terms { get; }

        public TermCache(string type, string[] terms)
        {
            Type = type;
            Terms = terms;
        }

        public IEnumerable<TermCache> Select(IRequest<TermCache> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var termCache = request.GetRequiredService<Requests.TermCache>();
            var resources = request.GetRequiredService<ResourceCollection>();
            foreach (var resource in resources)
            {
                yield return new TermCache
                (
                    type: resource.Type.GetRESTableTypeName(),
                    terms: termCache
                        .Where(pair => pair.Key.Type == resource.Type.GetRESTableTypeName())
                        .Select(pair => pair.Value.Key)
                        .ToArray()
                );
            }
        }

        public int Delete(IRequest<TermCache> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var termCache = request.GetRequiredService<Requests.TermCache>();
            var count = 0;
            foreach (var entity in request.GetInputEntities())
            {
                var keys = termCache
                    .Where(pair => pair.Key.Type == entity.Type)
                    .Select(pair => pair.Key)
                    .ToList();
                foreach (var key in keys)
                    termCache.Remove(key);
                count += 1;
            }
            return count;
        }
    }
}