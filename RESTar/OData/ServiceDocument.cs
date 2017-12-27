using System.Collections.Generic;
using System.Linq;

namespace RESTar.OData
{
    internal class ServiceDocument
    {
        public string name { get; }
        public string kind { get; }
        public string url { get; }

        private ServiceDocument(AvailableResource t) => (name, kind, url) = (t.Name, "EntitySet", t.Name);
        public static IEnumerable<ServiceDocument> Make(IEnumerable<AvailableResource> entities) => entities.Select(r => new ServiceDocument(r));
    }
}