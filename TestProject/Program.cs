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
            Handle.CUSTOM(8003, "FOO /getfoo", () => new Response {Body = "Foo body"});
            var request = (HttpWebRequest) WebRequest.Create("http://127.0.0.1:8003/getfoo");
            request.Timeout = 5000;
            request.Method = "FOO";
            try
            {
                var response = (HttpWebResponse) request.GetResponse();
                var reader = new StreamReader(response.GetResponseStream());
                var data = reader.ReadToEnd();
            }
            catch (WebException we)
            {
                var error = we.Message;
                // The operation has timed out
            }
        }
    }
}