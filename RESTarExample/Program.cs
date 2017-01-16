using RESTar;

namespace RESTarExample
{
    public class Program
    {
        public static void Main()
        {
            RESTarConfig.Init(prettyPrint: true);
            TestDatabase.Init();
        }
    }
}