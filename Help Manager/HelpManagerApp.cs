using Starcounter;
using RESTar;

namespace HelpManager
{
    public static class HelpManagerApp
    {
        public static void Main() => RESTarConfig.Init
        (
            port: 8010,
            requireApiKey: true,
            allowAllOrigins: true,
            configFilePath: @"C:\Mopedo\HelpManager.config"
        );
    }

    [Database, RESTar(Description = "Provides help and tips for how to use certain RESTar features")]
    public class HelpArticle
    {
        public string Topic;
        public string Body;
        public string SeeAlso;
    }
}