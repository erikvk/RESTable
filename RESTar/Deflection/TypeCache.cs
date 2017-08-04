using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using static RESTar.RESTarPresets;
#pragma warning disable 1591

namespace RESTar.Deflection
{
    [RESTar(ReadOnly)]
    public class TypeCache : ISelector<TypeCache>
    {
        public string Resource { get; set; }
        public string[] StaticProperties { get; set; }
        public string[] Terms { get; set; }

        public IEnumerable<TypeCache> Select(IRequest<TypeCache> request) => RESTarConfig.Resources
            .Select(r => new TypeCache
            {
                Resource = r.Name,
                StaticProperties = Dynamic.TypeCache.StaticPropertyCache[r.Name]
                    .Values
                    .Select(prop => prop.Name)
                    .ToArray(),
                Terms = Dynamic.TypeCache.TermCache
                    .Where(pair => pair.Key.Resource == r.Name)
                    .Select(pair => pair.Value.Key)
                    .ToArray()
            })
            .Where(request.Conditions);
    }
}