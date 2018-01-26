using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Dynamit;
using Newtonsoft.Json;
using RESTar;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Resources;
using Starcounter;
using static RESTar.Methods;
using static RESTar.Operators;

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

        public static void Main()
        {
            RESTarConfig.Init(9000, lineEndings: LineEndings.Windows, prettyPrint: true);
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

            var response1 = Http.POST("http://localhost:9000/rest/resource1", onesJson, null);
            var response2 = Http.POST("http://localhost:9000/rest/resource2", twosJson, null);
            var response3 = Http.POST("http://localhost:9000/rest/resource3", threesJson, null);
            var response4 = Http.POST("http://localhost:9000/rest/resource4", foursJson, null);
            var response5 = Http.POST("http://localhost:9000/rest/authresource", @"{""Id"": 1, ""Str"": ""Foogoo""}",
                new Dictionary<string, string>() {["password"] = "the password"});
            var response5fail = Http.POST("http://localhost:9000/rest/authresource", @"{""Id"": 2, ""Str"": ""Foogoo""}",
                new Dictionary<string, string>() {["password"] = "not the password"});

            Debug.Assert(response1?.IsSuccessStatusCode == true);
            Debug.Assert(response2?.IsSuccessStatusCode == true);
            Debug.Assert(response3?.IsSuccessStatusCode == true);
            Debug.Assert(response4?.IsSuccessStatusCode == true);
            Debug.Assert(response5?.IsSuccessStatusCode == true);
            Debug.Assert(response5fail?.StatusCode == 403);

            #endregion

            #region External source/destination inserts

            var resource1Url = "https://storage.googleapis.com/mopedo-web/resource1.json";
            var esourceresponse1 = Http.POST
            (
                uri: "http://localhost:9000/rest/resource1",
                body: null,
                headersDictionary: new Dictionary<string, string> {["Source"] = "GET " + resource1Url}
            );
            Debug.Assert(esourceresponse1?.IsSuccessStatusCode == true);

            var esourceresponse2 = Http.POST
            (
                uri: "http://localhost:9000/rest/MyDict",
                body: null,
                headersDictionary: new Dictionary<string, string> {["Source"] = "GET " + resource1Url}
            );
            Debug.Assert(esourceresponse2?.IsSuccessStatusCode == true);

            var esourceresponse3 = Http.POST
            (
                uri: "http://localhost:9000/rest/MyDict",
                body: null,
                headersDictionary: new Dictionary<string, string> {["Source"] = "GET /resource1"}
            );
            Debug.Assert(esourceresponse3?.IsSuccessStatusCode == true);

            var edestinationresponse1 = Http.GET
            (
                uri: "http://localhost:9000/rest/resource1",
                headersDictionary: new Dictionary<string, string> {["Destination"] = "POST /mydict"}
            );
            Debug.Assert(edestinationresponse1?.IsSuccessStatusCode == true);

            var edestinationresponse2 = Http.GET
            (
                uri: "http://localhost:9000/rest/mydict",
                headersDictionary: new Dictionary<string, string> {["Destination"] = "POST http://localhost:9000/rest/resource1"}
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

            var jsonResponse1 = Http.GET("http://localhost:9000/rest/restartester.resource1");
            var jsonResponse1view = Http.GET("http://localhost:9000/rest/resource1-myview");
            var jsonResponse2 = Http.GET("http://localhost:9000/rest/resource2");
            var jsonResponse3 = Http.GET("http://localhost:9000/rest/resource3");
            var jsonResponse4 = Http.GET("http://localhost:9000/rest/resource4");
            var jsonResponse4distinct = Http.GET("http://localhost:9000/rest/resource4//select=string&distinct=true");
            var jsonResponse4extreme = Http.GET
                ("http://localhost:9000/rest/resource4//add=datetime.day&select=datetime.day,datetime.month,string,string.length&order_desc=string.length&distinct=true");
            var jsonResponse5format = Http.GET
                ("http://localhost:9000/rest/resource4//add=datetime.day&select=datetime.day,datetime.month,string,string.length&order_desc=string.length&format=jsend&distinct=true");

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

            var headers = new Dictionary<string, string> {["Accept"] = "Excel"};
            var excelResponse1 = Http.GET("http://localhost:9000/rest/resource1", headersDictionary: headers);
            var excelResponse2 = Http.GET("http://localhost:9000/rest/resource2", headersDictionary: headers);
            var excelResponse3 = Http.GET("http://localhost:9000/rest/resource3", headersDictionary: headers);
            var excelResponse4 = Http.GET("http://localhost:9000/rest/resource4", headersDictionary: headers);

            Debug.Assert(excelResponse1?.IsSuccessStatusCode == true);
            Debug.Assert(excelResponse2?.IsSuccessStatusCode == true);
            Debug.Assert(excelResponse3?.IsSuccessStatusCode == true);
            Debug.Assert(excelResponse4?.IsSuccessStatusCode == true);

            #endregion

            #region POST Excel

            var headers2 = new Dictionary<string, string> {["Content-Type"] = "Excel"};
            var excelPostResponse1 = Http.POST("http://localhost:9000/rest/resource1", bodyBytes: excelResponse1.BodyBytes,
                headersDictionary: headers2);
            Debug.Assert(excelPostResponse1?.IsSuccessStatusCode == true);

            #endregion

            #region With conditions

            var conditionResponse1 = Http.GET("http://localhost:9000/rest/resource1/sbyte>0&byte!=200&datetime>2001-01-01");
            var conditionResponse2 = Http.GET("http://localhost:9000/rest/resource2/sbyte>0&byte!=200&datetime>2001-01-01");
            var conditionResponse3 = Http.GET("http://localhost:9000/rest/resource3/sbyte>0&byte!=200&datetime>2001-01-01");
            var conditionResponse4 = Http.GET("http://localhost:9000/rest/resource4/resource1.string!=aboo&resource2!=null");

            Debug.Assert(excelResponse1?.IsSuccessStatusCode == true);
            Debug.Assert(excelResponse2?.IsSuccessStatusCode == true);
            Debug.Assert(excelResponse3?.IsSuccessStatusCode == true);
            Debug.Assert(excelResponse4?.IsSuccessStatusCode == true);

            #endregion

            #region Internal requests

            var g = new Request<MyDict>().POST(() =>
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
            });

            var r1 = new Request<Resource1>(new Condition<Resource1>(nameof(Resource1.Sbyte), GREATER_THAN, 1));
            var r2 = new Request<Resource2>();
            var r3 = new Request<Resource3>();
            var r4 = new Request<Resource4>();
            var r5 = new Request<MyDict>();
            var cond = new Condition<MyDict>("Goo", EQUALS, false);
            r5.Conditions = new[] {cond};

            var res1 = r1.GET();
            var res2 = r2.GET();
            var res3 = r3.GET();
            var res4 = r4.GET();
            var res5 = r5.GET();
            var (excel, nrOfRows) = r5.GETExcel();

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

            Do.Schedule(() => Db.TransactAsync(() => new MyDict() {["Aaa"] = "Wook"}), TimeSpan.FromSeconds(10));

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

        public int Insert(IRequest<AuthResource> request) => request.GetEntities().Aggregate(0, (count, entity) =>
        {
            Items.Add(entity);
            return count += 1;
        });

        public int Delete(IRequest<AuthResource> request) => request.GetEntities()
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