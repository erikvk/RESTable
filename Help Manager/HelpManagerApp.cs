using RESTar;
using Starcounter;

namespace HelpManager
{
    public class HelpManagerApp
    {
        public static void Main()
        {
            RESTarConfig.Init
            (
                port: 8010,
                requireApiKey: true,
                allowAllOrigins: true,
                configFilePath: "C:\\Mopedo\\HelpManager.config"
            );
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class HelpArticle
    {
        public string Topic;
        public string Body;
        public string SeeAlso;
    }
}