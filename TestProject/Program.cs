using Starcounter;

// ReSharper disable All

namespace TestProject
{
    public class Program
    {
        public static void Main()
        {
            Handle.GET(8100, "/test", () => new Response
            {
                StatusCode = 200,
                StatusDescription = "OK",
                Headers = {["X-MySingleHeader"] = "Some value\r\nFoo: other value\r\nThird value"}
            });
            var response = Http.GET("http://localhost:8100/test");
            var fooHeader = response.Headers["Foo"];
            // other value
        }
    }
}