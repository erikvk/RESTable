using System.IO;
using System.Net;
using System.Text;
using Starcounter;

// ReSharper disable All

namespace TestProject
{
    public class Program
    {
        private const string data = "This is a really important string, that I would like to send using StreamedBody!";

        public static void Main()
        {
            var dataArray = Encoding.UTF8.GetBytes(data);
            var arrayResponse = new Response {BodyBytes = dataArray};
            var streamResponse = new Response {StreamedBody = new MemoryStream(dataArray)};

            Handle.GET(8003, "/withstream", () => streamResponse);
            Handle.GET(8003, "/witharray", () => arrayResponse);

            var arrayData = Http.GET("http://localhost:8003/witharray").BodyBytes;
            var streamData = Http.GET("http://localhost:8003/withstream").BodyBytes;

            var r = (HttpWebRequest) WebRequest.Create("http://localhost:8003/withstream");
            r.Method = "GET";
            var response = (HttpWebResponse) r.GetResponse();
            var reader = new StreamReader(response.GetResponseStream());
            var streamData2 = reader.ReadToEnd();
            // HTTP/1.1 404 OK\r\nServer: SC\r\nContent-Type: text/html;charset=utf-8\r\nCache-Contro

            var arraystring = Encoding.UTF8.GetString(arrayData);
            // "This is a really important string, that I would like to send using StreamedBody!"

            var streamstring = Encoding.UTF8.GetString(streamData);
            // System.Exception: ScErrAppsHttpParserIncorrect (SCERR14002): HTTP contains incorrect data.\r\nVersion: 2.3.1.7478.
            // \r\nHelp page: https://docs.starcounter.io/v/2.3.1/?q=SCERR14002.\r\n   at Starcounter.NodeTask.PerformSyncRequest() 
            // in C:\\TeamCity\\TeamCity10\\buildAgent\\work\\sc-9598\\Level1\\src\\Starcounter.Internal\\Rest\\NodeTask.cs:line 550
        }
    }
}