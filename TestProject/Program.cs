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
            var response1 = Http.GET
            (
                uri: "http://localhost:8080/test",
                headersDictionary: new Dictionary<string, string> {["Content-Type"] = "application/json"}
            );
            var body1 = response1.Body;
            // "was null"

            var webrequest = (HttpWebRequest) WebRequest.Create("http://localhost:8080/test");
            webrequest.Method = "GET";
            webrequest.ContentType = "application/json";
            var webResponse = (HttpWebResponse) webrequest.GetResponse();
            string _body;
            using (var reader = new StreamReader(webResponse.GetResponseStream()))
                _body = reader.ReadToEnd();
            var body2 = body1;
            // "was null"
        }
    }
}