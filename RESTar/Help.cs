using System;
using System.Collections.Generic;
using RESTar.Linq;
using RESTar.Serialization;
using static RESTar.Operators;
using static RESTar.RESTarMethods;

namespace RESTar
{
    /// <summary>
    /// Contains help articles for RESTar
    /// </summary>
    [RESTar(GET)]
    public class Help : ISelector<Help>
    {
        /// <summary>
        /// The topic of the help article
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// The body of the help article
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// The "see also" part of the help article
        /// </summary>
        public string SeeAlso { get; set; }

        private const string URL = "https://restarhelp.mopedo-drtb.com/rest/helparticle/";

        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
        public IEnumerable<Help> Select(IRequest<Help> request)
        {
            var topic = ((string) request.Conditions.Get("topic", EQUALS)?.Value)?.UriEncode();
            var uri = new Uri(URL + (topic != null ? $"topic={topic}" : ""));
            var headers = new Dictionary<string, string> {["Authorization"] = "apikey restar"};
            return HTTP.External(method: GET, uri: uri, headers: headers)?.Body?.Deserialize<Help[]>();
        }
    }
}