using System;
using System.Collections.Generic;
using RESTar.Http;
using RESTar.Linq;
using RESTar.Serialization;
using static RESTar.Methods;

namespace RESTar
{
    /// <summary>
    /// Contains help articles for RESTar itself
    /// </summary>
    [RESTar(GET, Description = description)]
    public class Help : ISelector<Help>
    {
        private const string description = "The Help resource contains help articles for RESTar itself.";
        private const string URL = "https://restarhelp.mopedo-drtb.com/rest?/helparticle";

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

        /// <inheritdoc />
        public IEnumerable<Help> Select(IRequest<Help> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var uri = new Uri($"{URL}/{request.Conditions.ToUriString()}");
            var headers = new Dictionary<string, string> {["Authorization"] = "apikey restar"};
            return HttpRequest.External(request, method: GET, uri: uri, headers: headers)?.Body?.Deserialize<Help[]>();
        }
    }
}