﻿using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using static RESTar.Method;

#pragma warning disable 1591

namespace RESTar.Admin
{
    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="IDeleter{T}" />
    /// <summary>
    /// The TermCache resource contains all the terms that RESTar has encountered 
    /// for a given resource type, for example in conditions.
    /// </summary>
    [RESTar(GET, DELETE, Description = description)]
    public class TermCache : ISelector<TermCache>, IDeleter<TermCache>
    {
        private const string description = "The TermCache resource contains all the terms that RESTar " +
                                           "has encountered for a given resource, for example in conditions.";

        public string Type { get; set; }
        public string[] Terms { get; set; }

        public IEnumerable<TermCache> Select(IQuery<TermCache> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            return RESTarConfig.Resources.Select(r => new TermCache
            {
                Type = r.Type.RESTarTypeName(),
                Terms = Deflection.Dynamic.TypeCache.TermCache
                    .Where(pair => pair.Key.Type == r.Type.RESTarTypeName())
                    .Select(pair => pair.Value.Key)
                    .ToArray()
            }).Where(query.Conditions);
        }

        public int Delete(IQuery<TermCache> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            var count = 0;
            query.GetEntities().ForEach(e =>
            {
                Deflection.Dynamic.TypeCache.TermCache
                    .Where(pair => pair.Key.Type == e.Type)
                    .Select(pair => pair.Key).ToList()
                    .ForEach(key => Deflection.Dynamic.TypeCache.TermCache.TryRemove(key, out var _));
                count += 1;
            });
            return count;
        }
    }
}