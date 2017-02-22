using Dynamit;
using RESTar;
using static RESTar.RESTarMethods;


namespace RESTarExample
{
    public class Program
    {
        public static void Main()
        {
            RESTarConfig.Init();
            TestDatabase.Init();
            ResourceHelper.Register<MyDict>(GET, POST);
        }
        
    }

    [DDictionary(typeof(MyPair))]
    public class MyDict : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new MyPair(dict, key, value);
        }
    }

    public class MyPair : DKeyValuePair
    {
        public MyPair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }
}