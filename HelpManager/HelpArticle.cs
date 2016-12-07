using System.Collections.Generic;
using RESTar;
using Starcounter;

namespace HelpManager
{
    [Database]
    public class HelpArticle
    {
        public string Topic;
        public string Body;
        public string SeeAlso;
    }
}
