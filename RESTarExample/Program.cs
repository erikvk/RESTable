using RESTar;

namespace RESTarExample
{
    public class Program
    {
        static void Main()
        {
            RESTarConfig.Init(httpPort: 8200, baseUri: "restar");
        }
    }
}