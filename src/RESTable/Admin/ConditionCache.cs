﻿using System;
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
    /// The ConditionCache resource contains all the conditions that RESTable has received 
    /// in requests for a given resource type.
    /// </summary>
    [RESTable(GET, DELETE, Description = description)]
    public class ConditionCache : ISelector<ConditionCache>, IDeleter<ConditionCache>
    {
        private const string description = "The ConditionCache resource contains all the conditions that RESTable has received" +
                                           "in requests for a given resource type.";

        private Type Type { get; }
        
        public string ResourceType { get; }
        public ICondition[] Conditions { get; }

        public ConditionCache(Type type, ICondition[] conditions)
        {
            Type = type;
            Conditions = conditions;
            ResourceType = type.GetRESTableTypeName();
        }

        public IEnumerable<ConditionCache> Select(IRequest<ConditionCache> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var resources = request.GetService<ResourceCollection>();
            foreach (var resource in resources)
            {
                var cache = (IConditionCache) request.GetService(typeof(ConditionCache<>).MakeGenericType(resource.Type));
                if (cache?.Count > 0)
                {
                    yield return new ConditionCache
                    (
                        type: resource.Type,
                        conditions: cache.ToArray()
                    );
                }
            }
        }

        public int Delete(IRequest<ConditionCache> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var resource in request.GetInputEntities())
            {
                var cache = (IConditionCache) request.GetService(typeof(ConditionCache<>).MakeGenericType(resource.Type));
                if (cache != null)
                {
                    cache.Clear();
                    count += 1;
                }
            }
            return count;
        }
    }
}