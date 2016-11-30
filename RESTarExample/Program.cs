using System;
using System.Collections;
using System.Runtime.Serialization;
using System.Collections.Generic;
using RESTar;
using Starcounter;

namespace RESTarExample
{
    public class Program
    {
        static void Main()
        {
            RESTarConfig.Init(httpPort: 8200, baseUri: "myuri");
        }
    }
}