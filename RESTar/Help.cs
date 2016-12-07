using System.Collections.Generic;
using Jil;
using RESTar;
using Starcounter;

namespace RESTar
{
    [Database]
    [RESTar(RESTarPresets.ReadOnly)]
    public class Help
    {
        public string Topic;
        public string Body;
        public string SeeAlso;

        public static string Get(string topic)
        {
            return HTTP.GET($"http://restarhelp.mopedo-drtb.com:8011/getarticle/{topic}").Body;
        }
    }
}