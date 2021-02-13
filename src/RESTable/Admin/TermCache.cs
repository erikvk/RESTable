using System;
using System.Collections.Generic;
using System.Linq;
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
            return RESTableConfig.Resources.Select(r => new TermCache
            {
                Type = r.Type.GetRESTableTypeName(),
                Terms = TypeCache.TermCache
                    .Where(pair => pair.Key.Type == r.Type.GetRESTableTypeName())
                    .Select(pair => pair.Value.Key)
                    .ToArray()
            });
        }

        public int Delete(IRequest<TermCache> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            request.GetInputEntities().ForEach(e =>
            {
                TypeCache.TermCache
                    .Where(pair => pair.Key.Type == e.Type)
                    .Select(pair => pair.Key).ToList()
                    .ForEach(key => TypeCache.TermCache.TryRemove(key, out _));
                count += 1;
            });
            return count;
        }
    }
}