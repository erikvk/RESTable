using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;
using RESTar.Linq;
using static RESTar.Methods;

namespace RESTar.OData
{
    [RESTar(GET)]
    internal sealed class ServiceDocument : ISelector<ServiceDocument>
    {
        public string name { get; }
        public string kind { get; }
        public string url { get; }

        private ServiceDocument(ITarget t) => (name, kind, url) = (t.Name, "EntitySet", t.Name);

        public IEnumerable<ServiceDocument> Select(IRequest<ServiceDocument> request) => RESTarConfig
            .AuthTokens[request.AuthToken]?.Keys
            .Where(r => r.IsGlobal && !r.IsInnerResource)
            .OrderBy(r => r.Name)
            .Select(r => new ServiceDocument(r))
            .Where(request.Conditions);
    }
}