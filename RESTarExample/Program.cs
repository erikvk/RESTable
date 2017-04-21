using System;
using System.Collections.Generic;
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
    public class MyResource : IInserter<MyResource>
    {
        public string Str;
        public int Inte;

        public int Insert(IEnumerable<MyResource> entities, IRequest request)
        {
            var count = 0;
            foreach (var entity in entities)
            {
                if (entity.Str == "ASD ASD")
                    throw new Exception("Invalid string");
                count += 1;
            }
            return count;
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class MyOther
    {
        public string Str;
        public Binary Binary;
    }
}