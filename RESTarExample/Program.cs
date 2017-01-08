using System;
using Dynamit;
using RESTar;
using Starcounter;

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