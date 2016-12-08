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

        public static IEnumerable<Help> Get(IEnumerable<Condition> conditions)
        {
            var topic = conditions.ValueForEquals("topic");
            return JSON.Deserialize<Help[]>(HTTP.GET("http://restarhelp.mopedo-drtb.com:8282/restar/helparticle/" +
                                                     (topic != null ? $"topic={topic}" : "")).Body);
        }
    }
}