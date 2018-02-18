using System.Collections.Generic;

namespace RESTar
{
    [RESTar(Methods.GET)]
    internal class Help : ISelector<Help>
    {
        private const string DocumentationUrl = "https://github.com/Mopedo/Home/tree/master/RESTar/Consuming%20a%20RESTar%20API";
        public string DocumentationAvailableAt { get; set; }
        public IEnumerable<Help> Select(IRequest<Help> request) => new[] {new Help {DocumentationAvailableAt = DocumentationUrl}};
    }
}