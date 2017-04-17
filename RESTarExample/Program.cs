using RESTar;
using Starcounter;

namespace RESTarExample
{
    public class Program
    {
        public static void Main()
        {
            RESTarConfig.Init
            (
                requireApiKey: true,
                allowAllOrigins: false,
                configFilePath: "C:\\Mopedo\\Mopedo.config"
            );
            TestDatabase.Init();

            // Activate the test database by setting the Active flag 
            // to true using a PATCH request over the REST API to the
            // RESTarExample.TestDatabase resource.
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class MyResource
    {
        public string Str;
        public int Inte;
    }
}