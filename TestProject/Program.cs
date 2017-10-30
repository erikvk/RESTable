using Starcounter;

// ReSharper disable All

namespace TestProject
{
    public class Program
    {
        public static void Main()
        {
            Handle.CUSTOM(8003, "REPORT /getfoo", () => new Response {Body = "Foo body"});
            var response = Http.CustomRESTRequest
            (
                method: "REPORT",
                uri: "http://127.0.0.1:8003/getfoo",
                body: default(string),
                headersDictionary: null,
                receiveTimeoutSeconds: 10
            );
            var data = response.Body;
            // System.IO.IOException: Remote host closed the connection.\r\n   
            // at Starcounter.NodeTask.PerformSyncRequest() 
            // in C:\\TeamCity\\BuildAgent\\work\\sc-10022\\Level1\\src\\Starcounter.Internal\\Rest\\NodeTask.cs:line 514

            var s = "";
        }
    }
}