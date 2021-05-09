using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using static RESTable.Method;

namespace RESTable
{
    /// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
    /// <inheritdoc cref="JObject" />
    /// <summary>
    /// The Echo resource is a test and utility resource that returns the 
    /// request conditions as an object.
    /// </summary>
    [RESTable(GET, AllowDynamicConditions = true, Description = description)]
    public class Echo : JObject, IAsyncSelector<Echo>
    {
        private const string description = "The Echo resource is a test and utility entity resource that " +
                                           "returns the request conditions as an entity.";

        private Echo(object thing) : base(thing) { }

        /// <inheritdoc />
        public async IAsyncEnumerable<Echo> SelectAsync(IRequest<Echo> request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            var members = request.Conditions
                .Select(c => new JProperty(c.Key, c.Value))
                .ToHashSet(EqualityComparer);
            var body = request.Body.Deserialize<JObject>();
            await foreach (var item in body.ConfigureAwait(false))
            foreach (var property in item.Properties())
                members.Add(property);
            var echo = new Echo(members);
            request.Conditions.Clear();
            var termCache = request.GetRequiredService<TermCache>();
            termCache.ClearTermsFor<Echo>();
            yield return echo;
        }
    }
}