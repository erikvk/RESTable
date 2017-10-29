using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using RESTar;
using RESTar.Linq;
using Starcounter;

#pragma warning disable 219
// ReSharper disable All

namespace RESTarTester
{
    public class Tester
    {
        public static void Main()
        {
            RESTarConfig.Init(9000);

            Db.SQL<Base>("SELECT t FROM RESTarTester.Base t").ForEach(b => Db.TransactAsync(b.Delete));

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

            Debug.Assert(response1?.IsSuccessStatusCode == true);
            Debug.Assert(response2?.IsSuccessStatusCode == true);
            Debug.Assert(response3?.IsSuccessStatusCode == true);
            Debug.Assert(response4?.IsSuccessStatusCode == true);

            #endregion

            #region JSON GET

            var request = (HttpWebRequest) WebRequest.Create("http://localhost:9000/rest/resource1");
            request.Method = "GET";
            var response = (HttpWebResponse) request.GetResponse();
            var rstream = response.GetResponseStream();
            var streamreader = new StreamReader(rstream);
            var data = streamreader.ReadToEnd();
            Debug.Assert(!string.IsNullOrWhiteSpace(data));

            var jsonResponse1 = Http.GET("http://localhost:9000/rest/resource1");
            var jsonResponse2 = Http.GET("http://localhost:9000/rest/resource2");
            var jsonResponse3 = Http.GET("http://localhost:9000/rest/resource3");
            var jsonResponse4 = Http.GET("http://localhost:9000/rest/resource4");

            Debug.Assert(jsonResponse1?.IsSuccessStatusCode == true);
            Debug.Assert(jsonResponse2?.IsSuccessStatusCode == true);
            Debug.Assert(jsonResponse3?.IsSuccessStatusCode == true);
            Debug.Assert(jsonResponse4?.IsSuccessStatusCode == true);

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

            var r1 = new Request<Resource1>(new Condition<Resource1>(nameof(Resource1.Sbyte), Operator.GREATER_THAN, 1));
            var r2 = new Request<Resource2>();
            var r3 = new Request<Resource3>();
            var r4 = new Request<Resource4>();

            var res1 = r1.GET();
            var res2 = r2.GET();
            var res3 = r3.GET();
            var res4 = r4.GET();

            #endregion

            var done = true;
        }
    }

    [Database]
    public abstract class Base
    {
    }

    [Database, RESTar]
    public class Resource1 : Base
    {
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
        [ReadOnly] public float? Float;
        [ReadOnly] public double? Double;
        [ReadOnly] public decimal? Decimal;
        public string String;
        public bool? Bool;
        public DateTime? DateTime;
        public Resource1 Resource1;
        public Resource2 Resource2;
    }
}