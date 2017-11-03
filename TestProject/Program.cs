using Starcounter;

// ReSharper disable All

namespace TestProject
{
    public class Program
    {
        public static void Main()
        {
            Handle.GET(8003, "/getfoo", () => new Response {Body = "ASDKASD KALSKASD MASD MASDKLAMSD LKMASD LKMASD LKMASDL KMASDL KMASD LKAMSD Foo body"});
            var response = Http.CustomRESTRequest
            (
                method: "GET",
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

            Db.Transact(() =>
            {
                new TestClass
                {
                    NonTransient = "Hee",
                    Transient = "Goo"
                };
            });

        }
//[Database]
//public class TestClass
//{
//    public string NonTransient { get; set; }
//    [Transient] private List<string> _transient;
//    public List<string> Transient
//    {
//        get => _transient;
//        set => _transient = value;
//    }
//}
    }

    [Database]
    public class TestClass
    {
        public string NonTransient { get; set; }
        public string Transient { get; set; }
    }
}