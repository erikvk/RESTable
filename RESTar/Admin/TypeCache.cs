using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using static RESTar.Methods;

#pragma warning disable 1591

namespace RESTar.Admin
{
    /// <summary>
    /// The TermCache resource contains all the terms that RESTar has encountered 
    /// for a given resource, for example in conditions.
    /// </summary>
    [RESTar(GET, DELETE, Description = description)]
    public class TermCache : ISelector<TermCache>, IDeleter<TermCache>
    {
        private const string description = "The TermCache resource contains all the terms that RESTar " +
                                           "has encountered for a given resource, for example in conditions.";

        public string Resource { get; set; }
        public string[] Terms { get; set; }

        public IEnumerable<TermCache> Select(IRequest<TermCache> request) => RESTarConfig.Resources
            .Select(r => new TermCache
            {
                Resource = r.Name,
                Terms = Deflection.Dynamic.TypeCache.TermCache
                    .Where(pair => pair.Key.Resource == r.Name)
                    .Select(pair => pair.Value.Key)
                    .ToArray()
            })
            .Where(request.Conditions);

        public int Delete(IEnumerable<TermCache> entities, IRequest<TermCache> request)
        {
            var count = 0;
            entities.ForEach(e =>
            {
                Deflection.Dynamic.TypeCache.TermCache
                    .Where(pair => pair.Key.Resource == e.Resource)
                    .Select(pair => pair.Key).ToList()
                    .ForEach(key => Deflection.Dynamic.TypeCache.TermCache.TryRemove(key, out var _));
                count += 1;
            });
            return count;
        }
    }
}