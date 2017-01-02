using System.Collections.Generic;
using Jil;

namespace RESTar
{
    [VirtualResource, RESTar(RESTarMethods.GET)]
    public class Help
    {
        public string Topic { get; set; }
        public string Body { get; set; }
        public string SeeAlso { get; set; }

        public static IEnumerable<Help> Select(IRequest request)
        {
            var topic = request.Conditions.ValueForEquals("topic");
            var response = HTTP.Request("GET", "http://restarhelp.mopedo-drtb.com:8282/restar/helparticle/" +
                                    (topic != null ? $"topic={topic}" : ""));
            return response.Body != null ? JSON.Deserialize<Help[]>(response.Body) : null;
        }
    }
}