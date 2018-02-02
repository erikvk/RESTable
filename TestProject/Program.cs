using System.Collections.Generic;
using System.IO;
using System.Net;
using Starcounter;

// ReSharper disable All

namespace TestProject
{
    public class Program
    {
        public static void Main()
        {
            Handle.GET("/test", (Request request) => request.ContentType ?? "was null");

            var s = ";";
        }
    }
}