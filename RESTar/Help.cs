using System;
using System.Collections.Generic;
using static RESTar.Operators;
using static RESTar.RESTarMethods;

namespace RESTar
{
    [RESTar(GET, Viewable = true)]
    public class Help : ISelector<Help>
    {
        public string Topic { get; set; }
        public string Body { get; set; }
        public string SeeAlso { get; set; }
        private const string URL = "https://restarhelp.mopedo-drtb.com/rest/helparticle/";

        public IEnumerable<Help> Select(IRequest request)
        {
            var topic = ((string) request.Conditions?["topic", EQUALS])?.UriEncode();
            var uri = new Uri(URL + (topic != null ? $"topic={topic}" : ""));
            var headers = new Dictionary<string, string> {["Authorization"] = "apikey restar"};
            return HTTP.ExternalRequest(method: GET, uri: uri, headers: headers)?.Body?.Deserialize<Help[]>();
        }
    }
}