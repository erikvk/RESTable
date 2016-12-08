using RESTar;
using Starcounter;

namespace HelpManager
{
    public class HelpManagerApp
    {
        public static void Main()
        {
            RESTarConfig.Init();
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndPrivateWrite)]
    public class HelpArticle
    {
        public string Topic;
        public string Body;
        public string SeeAlso;
    }
}