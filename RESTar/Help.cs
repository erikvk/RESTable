using System;
using System.Collections.Generic;
using System.Web;
using Jil;

namespace RESTar
{
    [RESTar(RESTarMethods.GET)]
    public class Help : ISelector<Help>
    {
        public string Topic { get; set; }
        public string Body { get; set; }
        public string SeeAlso { get; set; }

        public IEnumerable<Help> Select(IRequest request)
        {
            var topic = request.Conditions.ValueForEquals("topic")?.ToString();
            var response = HTTP.Request("GET", "http://restarhelp.mopedo-drtb.com:8282/restar/helparticle/" +
                                               (topic != null ? $"topic={HttpUtility.UrlEncode(topic)}" : ""));
            return response.Body != null
                ? JSON.Deserialize<Help[]>(response.Body, Options.ISO8601IncludeInherited)
                : null;
        }
    }
}