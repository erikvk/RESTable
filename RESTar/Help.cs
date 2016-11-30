using System.Collections.Generic;
using Starcounter;

namespace RESTar
{
    [Database]
    [RESTar(RESTarPresets.ReadOnly)]
    public class Help : Resource
    {
        public string Topic;
        public string Body;
        public string TagsString;

        public IEnumerable<string> Tags
        {
            get { return TagsString.Split(','); }
            set { TagsString = string.Join(",", value); }
        }

        public Help(string topic, string body, params string[] tags)
        {
            Topic = topic;
            Body = body;
            Tags = tags;
        }
    }
}
