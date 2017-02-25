using RESTar;


namespace RESTarExample
{
    public class Program
    {
        public static void Main()
        {
            RESTarConfig.Init();
            TestDatabase.Init();

            // Activate the test database by setting the Active flag 
            // to true using a PATCH request over the REST API to the
            // RESTarExample.TestDatabase resource.
        }   
    }
}