using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Dynamit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Results;
using Starcounter;
using static RESTar.Method;
using static RESTar.Operators;
using Context = RESTar.Context;

#pragma warning disable 618
#pragma warning disable 219

// ReSharper disable All

namespace RESTarTester
{
    public class Tester
    {
        private static decimal Time(Action action)
        {
            var s = Stopwatch.StartNew();
            for (var i = 1; i < 20000; i++)
                action();
            s.Stop();
            return s.ElapsedMilliseconds;
        }

        private static class Http
        {
            private static HttpClient HttpClient = new HttpClient();

            internal static HttpResponseMessage Request(string method, string uri, byte[] body = null, string contentType = "application/json",
                Dictionary<string, string> headers = null)
            {
                var message = new HttpRequestMessage(new HttpMethod(method), uri);
                if (body != null)
                    message.Content =
                        new ByteArrayContent(body) {Headers = {ContentType = MediaTypeHeaderValue.Parse(contentType ?? "application/json")}};
                if (headers != null)
                {
                    foreach (var header in headers) message.Headers.Add(header.Key, header.Value);
                }
                message.Headers.Add("RESTar-metadata", "full");
                return HttpClient.SendAsync(message).Result;
            }
        }

        public static void Main()
        {
            RESTarConfig.Init
            (
                port: 9000,
                lineEndings: LineEndings.Windows,
                prettyPrint: true,
                allowAllOrigins: false,
                configFilePath: @"C:\Mopedo\mopedo\Mopedo.config"
            );

            Db.SQL<Base>("SELECT t FROM RESTarTester.Base t").ForEach(b => Db.TransactAsync(b.Delete));
            Db.SQL<MyDict>("SELECT t FROM RESTarTester.MyDict t").ForEach(b => Db.TransactAsync(b.Delete));
            Db.SQL<MyDict2>("SELECT t FROM RESTarTester.MyDict2 t").ForEach(b => Db.TransactAsync(b.Delete));

            string onesJson = null;
            string twosJson = null;
            string threesJson = null;
            string foursJson = null;

            #region JSON generation

            var transaction = new Transaction();
            transaction.Scope(() =>
            {
                var ones = new[]
                {
                    new Resource1
                    {
                        Sbyte = 100,
                        Byte = 100,
                        Short = 100,
                        Ushort = 100,
                        Int = 100,
                        Uint = 100U,
                        Long = 100L,
                        Ulong = 100UL,
                        Float = 100.12F,
                        Double = 100.123,
                        Decimal = 100.123M,
                        String = "Swiooooo",
                        Bool = true,
                        DateTime = DateTime.Now
                    },
                    new Resource1
                    {
                        Sbyte = -100,
                        Byte = 100,
                        Short = -100,
                        Ushort = 100,
                        Int = -100,
                        Uint = 100U,
                        Long = -100L,
                        Ulong = 100UL,
                        Float = -100.12F,
                        Double = -100.123,
                        Decimal = -100.123M,
                        String = "Swiooooo",
                        Bool = false,
                        DateTime = DateTime.Now.AddMinutes(100)
                    }
                };
                var twos = new[]
                {
                    new Resource2
                    {
                        Sbyte = 100,
                        Byte = null,
                        Short = 100,
                        Ushort = null,
                        Int = 100,
                        Uint = null,
                        Long = 100L,
                        Ulong = null,
                        Float = 100.12F,
                        Double = null,
                        Decimal = 100.123M,
                        String = "Swiooooo",
                        Bool = null,
                        DateTime = DateTime.Now
                    },
                    new Resource2
                    {
                        Sbyte = null,
                        Byte = 100,
                        Short = null,
                        Ushort = 100,
                        Int = null,
                        Uint = 100U,
                        Long = null,
                        Ulong = 100UL,
                        Float = null,
                        Double = -100.123,
                        Decimal = null,
                        String = null,
                        Bool = false,
                        DateTime = null
                    }
                };
                var threes = new[]
                {
                    new Resource3
                    {
                        Sbyte = 100,
                        Byte = null,
                        Short = 100,
                        Ushort = null,
                        Int = 100,
                        Uint = null,
                        Long = 100L,
                        Ulong = null,
                        Float = 100.12F,
                        Double = null,
                        Decimal = 100.123M,
                        String = "Swiooooo",
                        Bool = null,
                        DateTime = DateTime.Now,
                        Resource1 = new Resource1
                        {
                            Sbyte = 100,
                            Byte = 100,
                            Short = 100,
                            Ushort = 100,
                            Int = 100,
                            Uint = 100U,
                            Long = 100L,
                            Ulong = 100UL,
                            Float = 100.12F,
                            Double = 100.123,
                            Decimal = 100.123M,
                            String = "Swiooooo",
                            Bool = true,
                            DateTime = DateTime.Now
                        },
                        Resource2 = new Resource2
                        {
                            Sbyte = -100,
                            Byte = 100,
                            Short = -100,
                            Ushort = 100,
                            Int = -100,
                            Uint = 100U,
                            Long = -100L,
                            Ulong = 100UL,
                            Float = -100.12F,
                            Double = -100.123,
                            Decimal = -100.123M,
                            String = "Swiooooo",
                            Bool = false,
                            DateTime = DateTime.Now.AddMinutes(100)
                        }
                    },
                    new Resource3
                    {
                        Sbyte = null,
                        Byte = 100,
                        Short = null,
                        Ushort = 100,
                        Int = null,
                        Uint = 100U,
                        Long = null,
                        Ulong = 100UL,
                        Float = null,
                        Double = -100.123,
                        Decimal = null,
                        String = null,
                        Bool = false,
                        DateTime = null,
                        Resource1 = null,
                        Resource2 = null
                    }
                };
                var fours = new[]
                {
                    new Resource4
                    {
                        Sbyte = 100,
                        Byte = null,
                        Short = 100,
                        Ushort = null,
                        Int = 100,
                        Uint = null,
                        Long = 100L,
                        Ulong = null,
                        Float = 100.12F,
                        Double = null,
                        Decimal = 100.123M,
                        String = "Swiooooo",
                        Bool = null,
                        DateTime = DateTime.Now,
                        Resource1 = new Resource1
                        {
                            Sbyte = 100,
                            Byte = 100,
                            Short = 100,
                            Ushort = 100,
                            Int = 100,
                            Uint = 100U,
                            Long = 100L,
                            Ulong = 100UL,
                            Float = 100.12F,
                            Double = 100.123,
                            Decimal = 100.123M,
                            String = "Swiooooo",
                            Bool = true,
                            DateTime = DateTime.Now
                        },
                        Resource2 = new Resource2
                        {
                            Sbyte = -100,
                            Byte = 100,
                            Short = -100,
                            Ushort = 100,
                            Int = -100,
                            Uint = 100U,
                            Long = -100L,
                            Ulong = 100UL,
                            Float = -100.12F,
                            Double = -100.123,
                            Decimal = -100.123M,
                            String = "Swiooooo",
                            Bool = false,
                            DateTime = DateTime.Now.AddMinutes(100)
                        }
                    },
                    new Resource4
                    {
                        Sbyte = null,
                        Byte = 100,
                        Short = null,
                        Ushort = 100,
                        Int = null,
                        Uint = 100U,
                        Long = null,
                        Ulong = 100UL,
                        Float = null,
                        Double = -100.123,
                        Decimal = null,
                        String = null,
                        Bool = false,
                        DateTime = null,
                        Resource1 = null,
                        Resource2 = null
                    }
                };

                onesJson = JsonConvert.SerializeObject(ones);
                twosJson = JsonConvert.SerializeObject(twos);
                threesJson = JsonConvert.SerializeObject(threes);
                foursJson = JsonConvert.SerializeObject(fours);
            });
            transaction.Rollback();

            #endregion

            #region Insertions

            var response1 = Http.Request("POST", "http://localhost:9000/rest/resource1", Encoding.UTF8.GetBytes(onesJson), null);
            var response2 = Http.Request("POST", "http://localhost:9000/rest/resource2", Encoding.UTF8.GetBytes(twosJson), null);
            var response3 = Http.Request("POST", "http://localhost:9000/rest/resource3", Encoding.UTF8.GetBytes(threesJson), null);
            var response4 = Http.Request("POST", "http://localhost:9000/rest/resource4", Encoding.UTF8.GetBytes(foursJson), null);
            var response5 = Http.Request("POST", "http://localhost:9000/rest/authresource",
                Encoding.UTF8.GetBytes(@"{""Id"": 1, ""Str"": ""Foogoo""}"),
                headers: new Dictionary<string, string>() {["password"] = "the password"});
            var response5fail = Http.Request("POST", "http://localhost:9000/rest/authresource",
                Encoding.UTF8.GetBytes(@"{""Id"": 2, ""Str"": ""Foogoo""}"),
                headers: new Dictionary<string, string>() {["password"] = "not the password"});

            Debug.Assert(response1?.IsSuccessStatusCode == true);
            Debug.Assert(response2?.IsSuccessStatusCode == true);
            Debug.Assert(response3?.IsSuccessStatusCode == true);
            Debug.Assert(response4?.IsSuccessStatusCode == true);
            Debug.Assert(response5?.IsSuccessStatusCode == true);
            Debug.Assert(response5fail?.StatusCode == (HttpStatusCode) 403);

            #endregion

            #region External source/destination inserts

            var resource1Url = "https://storage.googleapis.com/mopedo-web/resource1.json";
            var esourceresponse1 = Http.Request
            (
                method: "POST",
                uri: "http://localhost:9000/rest/resource1",
                body: null,
                headers: new Dictionary<string, string> {["Source"] = "GET " + resource1Url}
            );
            Debug.Assert(esourceresponse1?.IsSuccessStatusCode == true);

            var esourceresponse2 = Http.Request
            (
                method: "POST",
                uri: "http://localhost:9000/rest/MyDict",
                body: null,
                headers: new Dictionary<string, string> {["Source"] = "GET " + resource1Url}
            );
            Debug.Assert(esourceresponse2?.IsSuccessStatusCode == true);

            var esourceresponse3 = Http.Request
            (
                method: "POST",
                uri: "http://localhost:9000/rest/MyDict",
                body: null,
                headers: new Dictionary<string, string> {["Source"] = "GET /resource1"}
            );
            Debug.Assert(esourceresponse3?.IsSuccessStatusCode == true);

            var edestinationresponse1 = Http.Request
            (
                method: "GET",
                uri: "http://localhost:9000/rest/resource1",
                headers: new Dictionary<string, string> {["Destination"] = "POST /mydict"}
            );
            Debug.Assert(edestinationresponse1?.IsSuccessStatusCode == true);

            var edestinationresponse2 = Http.Request
            (
                method: "GET",
                uri: "http://localhost:9000/rest/mydict",
                headers: new Dictionary<string, string> {["Destination"] = "POST http://localhost:9000/rest/resource1"}
            );
            Debug.Assert(edestinationresponse2?.IsSuccessStatusCode == true);

            #endregion

            #region JSON GET

            var request = (HttpWebRequest) WebRequest.Create("http://localhost:9000/rest/resource1");
            request.Method = "GET";
            var response = (HttpWebResponse) request.GetResponse();
            var rstream = response.GetResponseStream();
            var streamreader = new StreamReader(rstream);
            var data = streamreader.ReadToEnd();
            Debug.Assert(!string.IsNullOrWhiteSpace(data));

            var jsonResponse1 = Http.Request("GET", "http://localhost:9000/rest/restartester.resource1");
            var jsonResponse1view = Http.Request("GET", "http://localhost:9000/rest/resource1-myview");
            var jsonResponse2 = Http.Request("GET", "http://localhost:9000/rest/resource2");
            var jsonResponse3 = Http.Request("GET", "http://localhost:9000/rest/resource3");
            var jsonResponse4 = Http.Request("GET", "http://localhost:9000/rest/resource4");
            var jsonResponse4distinct = Http.Request("GET", "http://localhost:9000/rest/resource4//select=string&distinct=true");
            var jsonResponse4extreme = Http.Request("GET",
                "http://localhost:9000/rest/resource4//add=datetime.day&select=datetime.day,datetime.month,string,string.length&order_desc=string.length&distinct=true");
            var jsonResponse5format = Http.Request("GET",
                "http://localhost:9000/rest/resource4//add=datetime.day&select=datetime.day,datetime.month,string,string.length&order_desc=string.length&format=jsend&distinct=true");

            var head = Http.Request("HEAD", "http://localhost:9000/rest/resource1//distinct=true");
            var report = Http.Request("REPORT", "http://localhost:9000/rest/resource1//distinct=true");

            Debug.Assert(jsonResponse1?.IsSuccessStatusCode == true);
            Debug.Assert(jsonResponse1view?.IsSuccessStatusCode == true);
            Debug.Assert(jsonResponse2?.IsSuccessStatusCode == true);
            Debug.Assert(jsonResponse3?.IsSuccessStatusCode == true);
            Debug.Assert(jsonResponse4?.IsSuccessStatusCode == true);
            Debug.Assert(jsonResponse4distinct?.IsSuccessStatusCode == true);
            Debug.Assert(jsonResponse4extreme?.IsSuccessStatusCode == true);
            Debug.Assert(jsonResponse5format?.IsSuccessStatusCode == true);

            #endregion

            #region GET Excel

            var headers = new Dictionary<string, string> {["Accept"] = "application/restar-excel"};
            var excelResponse1 = Http.Request("GET", "http://localhost:9000/rest/resource1", headers: headers);
            var excelResponse2 = Http.Request("GET", "http://localhost:9000/rest/resource2", headers: headers);
            var excelResponse3 = Http.Request("GET", "http://localhost:9000/rest/resource3", headers: headers);
            var excelResponse4 = Http.Request("GET", "http://localhost:9000/rest/resource4", headers: headers);

            Debug.Assert(excelResponse1?.IsSuccessStatusCode == true);
            Debug.Assert(excelResponse2?.IsSuccessStatusCode == true);
            Debug.Assert(excelResponse3?.IsSuccessStatusCode == true);
            Debug.Assert(excelResponse4?.IsSuccessStatusCode == true);

            #endregion

            #region POST Excel

            var excelbody = excelResponse1.Content.ReadAsByteArrayAsync().Result;
            var excelPostResponse1 = Http.Request("POST", "http://localhost:9000/rest/resource1", body: excelbody,
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            Debug.Assert(excelPostResponse1?.IsSuccessStatusCode == true);

            var webrequest = (HttpWebRequest) WebRequest.Create("http://localhost:9000/rest/resource1");
            webrequest.Method = "POST";
            webrequest.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            using (var str = webrequest.GetRequestStream())
            using (var stream = new MemoryStream(excelbody))
                stream.CopyTo(str);
            var webResponse = (HttpWebResponse) webrequest.GetResponse();
            string _body;
            using (var reader = new StreamReader(webResponse.GetResponseStream()))
                _body = reader.ReadToEnd();
            Debug.Assert(webResponse.StatusCode < HttpStatusCode.BadRequest);

            #endregion

            #region With conditions

            var conditionResponse1 = Http.Request("GET", "http://localhost:9000/rest/resource1/sbyte>=0&byte!=200&datetime>2001-01-01");
            var conditionResponse2 = Http.Request("GET", "http://localhost:9000/rest/resource2/sbyte<=10&byte!=200&datetime>2001-01-01");
            var conditionResponse3 = Http.Request("GET", "http://localhost:9000/rest/resource3/sbyte>0&byte!=200&datetime>2001-01-01");
            var conditionResponse4 = Http.Request("GET", "http://localhost:9000/rest/resource4/resource1.string!=aboo&resource2!=null");

            Debug.Assert(excelResponse1?.IsSuccessStatusCode == true);
            Debug.Assert(excelResponse2?.IsSuccessStatusCode == true);
            Debug.Assert(excelResponse3?.IsSuccessStatusCode == true);
            Debug.Assert(excelResponse4?.IsSuccessStatusCode == true);

            #endregion

            #region Internal requests

            var context = Context.Root;
            var g = context.CreateRequest<MyDict>(POST);
            g.Selector = () =>
            {
                dynamic d = new MyDict();
                d.Hej = "123";
                d.Foo = 3213M;
                d.Goo = true;
                dynamic v = new MyDict();
                v.Hej = "123";
                v.Foo = 3213M;
                v.Goo = false;
                dynamic x = new MyDict();
                x.Hej = "123";
                x.Foo = 3213M;
                x.Goo = false;
                return new MyDict[] {d, v, x};
            };
            var result = g.Result;
            Debug.Assert(result is InsertedEntities ie && ie.InsertedCount == 3);

            var g2 = context.CreateRequest<MyDict>(POST);
            dynamic d2 = new JObject();
            d2.Hej = "123";
            d2.Foo = 3213M;
            d2.Goo = true;
            dynamic v2 = new JObject();
            v2.Hej = "123";
            v2.Foo = 3213M;
            v2.Goo = false;
            dynamic x2 = new JObject();
            x2.Hej = "123";
            x2.Foo = 3213M;
            x2.Goo = false;
            var arr2 = new[] {d2, v2, x2};
            g2.SetBody(arr2);
            var result2 = g2.Result;
            Debug.Assert(result2 is InsertedEntities ie2 && ie2.InsertedCount == 3);

            var g5 = context.CreateRequest<MyDict>(POST);
            dynamic d5 = new JObject();
            d5.Hej = "123";
            d5.Foo = 3213M;
            d5.Goo = true;
            dynamic v5 = new JObject();
            v5.Hej = "123";
            v5.Foo = 3213M;
            v5.Goo = false;
            dynamic x5 = new JObject();
            x5.Hej = "123";
            x5.Foo = 3213M;
            x5.Goo = false;
            var arr5 = new[] { d5, v5, x5 };
            g5.SetBody(arr5, ContentType.Excel);
            var result5 = g5.Result;
            Debug.Assert(result5 is InsertedEntities ie5 && ie5.InsertedCount == 3);

            var g3 = context.CreateRequest<MyDict>(POST);
            var d3 = new
            {
                Hej = "123",
                Foo = 3213M,
                Goo = true
            };
            g3.SetBody(d3);
            var result3 = g3.Result;
            Debug.Assert(result3 is InsertedEntities ie3 && ie3.InsertedCount == 1);

            var g4 = context.CreateRequest<MyDict>(POST);
            var d4 = JsonConvert.SerializeObject(new
            {
                Hej = "123",
                Foo = 3213M,
                Goo = true
            });
            g4.SetBody(d4);
            var result4 = g4.Result;
            Debug.Assert(result4 is InsertedEntities ie4 && ie4.InsertedCount == 1);


            var r1Cond = new Condition<Resource1>(nameof(Resource1.Sbyte), GREATER_THAN, 1);
            var r1 = context.CreateRequest<Resource1>(GET);
            r1.Conditions.Add(r1Cond);

            var r2 = context.CreateRequest<Resource2>(GET);
            var r3 = context.CreateRequest<Resource3>(GET);
            var r4 = context.CreateRequest<Resource4>(GET);
            var r6 = context.CreateRequest<Aggregator>(GET);
            r6.SetBody(new
            {
                A = "REPORT /resource",
                B = new[] {"REPORT /resource", "REPORT /resource"}
            });
            var r5 = context.CreateRequest<Resource1>(GET);
            var cond = new Condition<Resource1>("SByte", GREATER_THAN, 2);
            r5.Conditions.Add(cond);
            r5.Headers.Accept = RESTar.ContentType.Excel;

            var res1 = r1.Result.Serialize();
            var res2 = r2.Result.Serialize();
            var res3 = r3.Result.Serialize();
            var res4 = r4.Result.Serialize();
            var res5 = r5.Result.Serialize();
            var res6 = r6.Result.Serialize();

            Debug.Assert(res5.Headers.ContentType == RESTar.ContentType.Excel);
            Debug.Assert(res5.Body.Length > 1);

            Db.TransactAsync(() =>
            {
                var x = new MyDict
                {
                    ["Hej"] = "123",
                    ["Foo"] = 3213M,
                    ["Goo"] = false
                };
                foreach (Resource1 asd in Db.SQL<Resource1>("SELECT t FROM RESTarTester.Resource1 t"))
                {
                    asd.MyDict = x;
                }
            });

            Do.Schedule(() => Db.TransactAsync(() => new MyDict() {["Aaa"] = "Wook"}), TimeSpan.FromSeconds(10)).Wait();

            DatabaseIndex.Register<MyDict2>("MyFineIdex", "R");

            Db.TransactAsync(() =>
            {
                new MyDict2
                {
                    ["Snoo"] = 123,
                    R = new Resource1
                    {
                        Byte = 123,
                        String = "Googfoo"
                    }
                };
            });

            var byInternalSource = Http.Request("POST", "http://localhost:9000/rest/resource3", null,
                headers: new Dictionary<string, string> {["Source"] = "GET /resource3"});

            #endregion

            #region Remote requests

            var remoteContext = Context.Remote("http://localhost:9000/rest");
            var remoteRequest = remoteContext.CreateRequest(GET, "/resource1");
            var remoteResult = remoteRequest.Result;
            Debug.Assert(remoteResult is IEntities rement && rement.EntityCount > 1);

            #endregion

            #region OPTIONS

            var optionsResponse1 = Http.Request("OPTIONS", "http://localhost:9000/rest/resource1", null,
                headers: new Dictionary<string, string> {["Origin"] = "https://fooboo.com/thingy"});
            Debug.Assert(optionsResponse1?.IsSuccessStatusCode == true);
            var optionsResponse2 = Http.Request("OPTIONS", "http://localhost:9000/rest/resource1", null,
                headers: new Dictionary<string, string> {["Origin"] = "https://fooboo.com/invalid"});
            Debug.Assert(optionsResponse2?.IsSuccessStatusCode == false);

            #endregion

            #region XML

            Debug.Assert(Http.Request("GET", "http://localhost:9000/rest/resource2",
                headers: new Dictionary<string, string> {["Accept"] = "application/xml"}).IsSuccessStatusCode);

            #endregion

            #region Error triggers

            Debug.Assert(Http.Request("GET", "http://localhost:9000/rest/x9").StatusCode == HttpStatusCode.NotFound);
            Debug.Assert(Http.Request("GET", "http://localhost:9000/rest/resource1/bfa=1").StatusCode == HttpStatusCode.NotFound);
            Debug.Assert(Http.Request("GET", "http://localhost:9000/rest/resource1//limit=foo").StatusCode == HttpStatusCode.BadRequest);
            Debug.Assert(Http.Request("POST", "http://localhost:9000/rest/resource1/bfa=1").StatusCode == HttpStatusCode.NotFound);
            Debug.Assert(Http.Request("POST", "http://localhost:9000/rest/resource1").StatusCode == HttpStatusCode.BadRequest);
            Debug.Assert(Http.Request("PATCH", "http://localhost:9000/rest/resource1").StatusCode == HttpStatusCode.BadRequest);
            Debug.Assert(Http.Request("POST", "http://localhost:9000/rest/myres", new byte[] {1, 2, 3}).StatusCode == HttpStatusCode.MethodNotAllowed);

            #endregion

            var done = true;
        }
    }

    public enum Things
    {
        a,
        b,
        c
    }

    [Database, RESTar]
    public class MyTestClass
    {
        [RESTarMember(
            order: 5
        )]
        public string STR { get; set; }

        [RESTarMember(
            order: 4,
            readOnly: true
        )]
        public int INT { get; set; }

        [RESTarMember(
            name: "BLOO",
            allowedOperators: EQUALS | GREATER_THAN
        )]
        public bool BOOL { get; set; }

        public Hoo Hoo => new Hoo {Goo = "Swoo", Ioo = 321};

        [RESTarMember()] public object Goo => new FooGoo { };

        [RESTarMember()] public FooGoo FooGoo => new FooGoo { };

        [RESTarMember(
            hide: true
        )]
        public int HENGTH => STR.Length;

        [RESTarMember(
            hideIfNull: true,
            skipConditions: true
        )]
        public string FOO { get; set; }

        [RESTarMember(
            ignore: true
        )]
        public int LENGTH => STR.Length;
    }

    public class FooGoo
    {
        public override string ToString() => "It's wooooorkiiing";
    }

    public class Hoo
    {
        public string Goo;
        public int Ioo { get; set; }
    }

    [RESTar(GET, AllowDynamicConditions = true, FlagStaticMembers = true)]
    public class MyRes : Dictionary<string, object>, ISelector<MyRes>
    {
        public Things T { get; set; }

        public IEnumerable<MyRes> Select(IRequest<MyRes> request)
        {
            Things thing = request.Conditions.Get("$T", EQUALS).Value;
            var other = request.Conditions.Get("V", EQUALS).Value;
            return new[] {new MyRes {["T"] = thing, ["V"] = other}};
        }
    }

    [RESTar]
    public class MyDict : DDictionary, IDDictionary<MyDict, MyDictKvp>
    {
        public MyDictKvp NewKeyPair(MyDict dict, string key, object value = null)
        {
            return new MyDictKvp(dict, key, value);
        }
    }

    public class MyDictKvp : DKeyValuePair
    {
        public MyDictKvp(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [RESTar]
    public class MyDict2 : DDictionary, IDDictionary<MyDict2, MyDict2Kvp>
    {
        public Resource1 R;

        public MyDict2Kvp NewKeyPair(MyDict2 dict, string key, object value = null)
        {
            return new MyDict2Kvp(dict, key, value);
        }
    }

    public class MyDict2Kvp : DKeyValuePair
    {
        public MyDict2Kvp(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    [RESTar(GET)]
    public class AsyncTest : ISelector<AsyncTest>
    {
        public bool Hej { get; set; }

        public IEnumerable<AsyncTest> Select(IRequest<AsyncTest> request)
        {
            async Task<AsyncTest> get()
            {
                await Task.Delay(3000);
                return new AsyncTest {Hej = true};
            }

            return new[] {get().Result};
        }
    }

    [RESTar]
    public class Wrapper : ResourceWrapper<Base>, ISelector<Base>
    {
        public IEnumerable<Base> Select(IRequest<Base> request)
        {
            return Db.SQL<Resource1>("SELECT t FROM RESTarTester.Resource1 t");
        }
    }

    [RESTar(GET, POST, DELETE)]
    public class AuthResource : ISelector<AuthResource>, IInserter<AuthResource>, IDeleter<AuthResource>,
        IAuthenticatable<AuthResource>, IValidatable
    {
        #region Schema

        public int Id { get; set; }
        public string Str { get; set; }

        #endregion

        private static List<AuthResource> Items = new List<AuthResource>();

        public IEnumerable<AuthResource> Select(IRequest<AuthResource> request) => Items.Where(request.Conditions);

        public int Insert(IRequest<AuthResource> request) => request.GetInputEntities().Aggregate(0, (count, entity) =>
        {
            Items.Add(entity);
            return count += 1;
        });

        public int Delete(IRequest<AuthResource> request) => request.GetInputEntities()
            .Aggregate(0, (count, entity) => count += Items.RemoveAll(i => i.Id == entity.Id));

        public AuthResults Authenticate(IRequest<AuthResource> request)
        {
            var password = request.Headers["password"];
            return (password == "the password", "Invalid password!");
        }

        public bool IsValid(out string invalidReason)
        {
            if (Items.Any(c => c.Id == Id))
            {
                invalidReason = "No no";
                return false;
            }
            invalidReason = null;
            return true;
        }
    }

    [Database]
    public abstract class Base { }

    public enum MyEnum
    {
        A,
        B,
        C,
        D,
        E,
        F
    }

    [Database, RESTar]
    public class Resource1 : Base
    {
        [RESTarView]
        public class MyView : ISelector<Resource1>
        {
            public bool Active { get; set; }

            public IEnumerable<Resource1> Select(IRequest<Resource1> request)
            {
                if (request.Conditions.Get("Active", EQUALS)?.Value == true)
                    return Db.SQL<Resource1>("SELECT t FROM RESTarTester.Resource1 t")
                        .Where(request.Conditions);
                return null;
            }
        }

        public sbyte Sbyte;
        public byte Byte;
        public short Short;
        public ushort Ushort;
        public int Int;
        public uint Uint;
        public long Long;
        public ulong Ulong;
        public float Float;
        public double Double;
        public decimal Decimal;
        public string String;
        public bool Bool;
        public DateTime DateTime;
        public MyDict MyDict;
    }

    [Database, RESTar]
    public class Resource2 : Base
    {
        public MyEnum Enum => MyEnum.B;
        public MyEnum[] Enums => new[] {MyEnum.B, MyEnum.A, MyEnum.E, MyEnum.C};
        public DateTime[] Dts => new[] {System.DateTime.Now, System.DateTime.MaxValue, System.DateTime.MinValue};
        public decimal[] Dcs => new[] {1M, 123.321M, 32123.123321M, -123321.12321M};

        public sbyte? Sbyte;
        public byte? Byte;
        public short? Short;
        public ushort? Ushort;
        public int? Int;
        public uint? Uint;
        public long? Long;
        public ulong? Ulong;
        public float? Float;
        public double? Double;
        public decimal? Decimal;
        public string String;
        public bool? Bool;
        public DateTime? DateTime;
    }

    [Database, RESTar]
    public class Resource3 : Base
    {
        public sbyte? Sbyte;
        public byte? Byte;
        public short? Short;
        public ushort? Ushort;
        public int? Int;
        public uint? Uint;
        public long? Long;
        public ulong? Ulong;
        public float? Float;
        public double? Double;
        public decimal? Decimal;
        public string String;
        public bool? Bool;
        public DateTime? DateTime;
        public Resource1 Resource1;
        public Resource2 Resource2;
    }

    [Database, RESTar]
    public class Resource4 : Base
    {
        [IgnoreDataMember] public sbyte? Sbyte;
        [IgnoreDataMember] public byte? Byte;
        [IgnoreDataMember] public short? Short;
        [IgnoreDataMember] public ushort? Ushort;
        [DataMember(Name = "RENAMED_Int")] public int? Int;
        [DataMember(Name = "RENAMED_Uint")] public uint? Uint;
        [DataMember(Name = "RENAMED_Long")] public long? Long;
        [DataMember(Name = "RENAMED_Ulong")] public ulong? Ulong;
        public float? Float;
        public double? Double;
        public decimal? Decimal;
        public string String;
        public bool? Bool;
        public DateTime? DateTime;
        public Resource1 Resource1;
        public Resource2 Resource2;
    }
}