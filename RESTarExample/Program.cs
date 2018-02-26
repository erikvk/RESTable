using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar;
using RESTar.Resources;
using RESTar.ResourceTemplates;
using Starcounter;

#pragma warning disable 1591
// ReSharper disable All

namespace RESTarExample
{
    public static class Program
    {
        public static void Main()
        {
            RESTarConfig.Init
            (
                uri: "/rest",
                requireApiKey: true,
                allowAllOrigins: false,
                configFilePath: @"C:\Mopedo\mopedo\Mopedo.config",
                lineEndings: LineEndings.Linux
            );
        }
    }

    [RESTar(Methods.GET)]
    public class Thing : ISelector<Thing>
    {
        public IEnumerable<Thing> Select(IRequest<Thing> request)
        {
            throw new NotImplementedException();
        }

        [RESTar]
        public class MyOptionsTerminal : OptionsTerminal
        {
            protected override IEnumerable<Option> GetOptions()
            {
                return new[] { new Option("Foo", "a foo", strings => { }) };
            }
        }
    }


    #region Stuff

    #region Solution 1

    public class MyStaticConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var myStatic = (MyStatic) value;
            writer.WriteStartObject();
            writer.WritePropertyName("myString");
            writer.WriteValue(myStatic.MyString);
            writer.WritePropertyName("myInt");
            writer.WriteValue(myStatic.MyInt);
            writer.WritePropertyName("myDateTime");
            writer.WriteValue(myStatic.MyDateTime);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
            throw new NotImplementedException();

        public override bool CanRead { get; } = false;
        public override bool CanWrite { get; } = true;
        public override bool CanConvert(Type objectType) => objectType == typeof(MyStatic);
    }

    [Database, RESTar, JsonConverter(typeof(MyStaticConverter))]
    public class MyStatic
    {
        public string MyString { get; set; }
        public int MyInt { get; set; }
        public DateTime MyDateTime { get; set; }
    }

    #endregion

    #region Solution 2

    [Database, RESTar(Interface = typeof(IVersion1))]
    public class MyStatic2 : MyStatic2.IVersion1
    {
        public string MyString { get; set; }
        public int MyInt { get; set; }
        public DateTime MyDateTime { get; set; }

        public bool objo { get; }

        #region Version1 interface

        public interface IVersion1
        {
            string _ICanCallThisWhateverString { get; }
            int __ThisIsMyINt { get; set; }
            DateTime AndTheDateTime_ffs { get; set; }
            bool Objectbo { get; }
        }

        string IVersion1._ICanCallThisWhateverString
        {
            get => MyString;
        }

        int IVersion1.__ThisIsMyINt
        {
            get => MyInt;
            set
            {
                MyDateTime = DateTime.MaxValue;
                MyInt = value;
            }
        }

        DateTime IVersion1.AndTheDateTime_ffs
        {
            get => MyDateTime;
            set => MyDateTime = value;
        }

        public bool Objectbo => !objo;

        #endregion
    }

    #endregion

    [Database, RESTar]
    public class Static
    {
        [RESTarMember(hideIfNull: true)] public int Swoo { get; set; }
        private string _str;

        public string Str
        {
            get => _str;
            set
            {
                if (value == "nono")
                    throw new Exception("Oh no no!");
                else _str = value;
            }
        }

        public dynamic XOXO => Str;

        public int Int { get; set; }
    }

    [RESTar(Methods.GET)]
    public class SemiDynamic : JObject, ISelector<SemiDynamic>
    {
        public string InputStr { get; set; } = "Goo";
        public int Int { get; set; } = 100;

        public IEnumerable<SemiDynamic> Select(IRequest<SemiDynamic> request)
        {
            return new[]
            {
                new SemiDynamic
                {
                    ["Str"] = "123",
                    ["Int"] = 0,
                    ["Count"] = -1230
                },
                new SemiDynamic
                {
                    ["Str"] = "ad123",
                    ["Int"] = 14
                },
                new SemiDynamic
                    {["Str"] = "123"},
                new SemiDynamic
                {
                    ["Str"] = "1ds23",
                    ["Int"] = 200
                }
            };
        }
    }

    [RESTar(Methods.GET)]
    public class SemiDynamic2 : Dictionary<string, object>, ISelector<SemiDynamic2>
    {
        public IEnumerable<SemiDynamic2> Select(IRequest<SemiDynamic2> request)
        {
            return new[]
            {
                new SemiDynamic2
                {
                    ["Str"] = "ad123",
                    ["Int"] = 14
                },
                new SemiDynamic2
                    {["Str"] = "123"},
                new SemiDynamic2
                {
                    ["Str"] = "1ds23",
                    ["Int"] = 200
                }
            };
        }
    }

    [RESTar(Methods.GET, AllowDynamicConditions = true)]
    public class AllDynamic : JObject, ISelector<AllDynamic>
    {
        public string Str { get; set; }
        public int Int { get; set; }

        public IEnumerable<AllDynamic> Select(IRequest<AllDynamic> request)
        {
            return new[]
            {
                new AllDynamic {["Str"] = "123", ["Int"] = 120},
                new AllDynamic {["Str"] = 232, ["Int"] = 13},
                new AllDynamic {["Str"] = 232, ["Int"] = -123},
                new AllDynamic {["AStr"] = "ASD", ["Int"] = 5}
            };
        }
    }

    [RESTar]
    public class DDictThing : DDictionary, IDDictionary<DDictThing, DDictKeyValuePair>
    {
        public string Str { get; set; }
        public int Int { get; set; }

        public DDictKeyValuePair NewKeyPair(DDictThing dict, string key, object value = null)
        {
            return new DDictKeyValuePair(dict, key, value);
        }
    }

    public class DDictKeyValuePair : DKeyValuePair
    {
        public DDictKeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    #region Random resources

    [RESTar]
    public class MyThing : ResourceWrapper<Table> { }

    [Database]
    public class Table
    {
        public string STR;
        public DateTime? DT;
        public DateTime DT2;
    }

    [RESTar(Methods.GET, Singleton = true)]
    public class MyTestResource : Dictionary<string, dynamic>, ISelector<MyTestResource>
    {
        public IEnumerable<MyTestResource> Select(IRequest<MyTestResource> request)
        {
            return new[]
            {
                new MyTestResource
                {
                    ["T"] = 1,
                    ["G"] = "asd",
                    ["Goo"] = 10
                },
                new MyTestResource
                {
                    ["T"] = 5,
                    ["G"] = "asd"
                },
                new MyTestResource
                {
                    ["T"] = -1,
                    ["G"] = "asd",
                    ["Boo"] = -10,
                    ["ASD"] = 123312
                },
                new MyTestResource
                {
                    ["T"] = 10,
                    ["G"] = "asd",
                    ["Boo"] = -10,
                    ["ASD"] = 123312,
                    ["Count"] = 30
                }
            };
        }
    }

    [Database, RESTar]
    public class MyResource
    {
        public int MyId;
        public decimal MyDecimal;
        public string MyMember;
        public string SomeMember;

        [RESTar(Methods.GET, Description = "Returns a fine object")]
        public class Get : JObject, ISelector<Get>
        {
            public IEnumerable<Get> Select(IRequest<Get> request) => new[] {new Get {["Soo"] = 123}};
        }
    }


    [Database, RESTar]
    public class MyClass
    {
        public int MyInt;
        private int prInt;

        public int OtherInt
        {
            get => prInt;
            set => prInt = value;
        }

        public MyResource Resource { get; }

        public int ThirdInt
        {
            get => prInt;
            set
            {
                if (value > 10)
                    prInt = value;
                else prInt = 0;
            }
        }
    }

    [RESTar]
    public class R : IInserter<R>, ISelector<R>, IUpdater<R>, IDeleter<R>
    {
        public string S { get; set; }
        public string[] Ss { get; set; }

        public int Insert(IRequest<R> request)
        {
            var entities = request.GetEntities();
            return entities.Count();
        }

        public IEnumerable<R> Select(IRequest<R> request)
        {
            return new[] {new R {S = "Swoo", Ss = new[] {"S", "Sd"}}};
        }

        public int Update(IRequest<R> request)
        {
            var entities = request.GetEntities();
            return entities.Count();
        }

        public int Delete(IRequest<R> request)
        {
            var entities = request.GetEntities();
            return entities.Count();
        }
    }

    public enum EE
    {
        A,
        B,
        C
    }

    [Database, RESTar]
    public class MyOther
    {
        public string Str;
    }

    [DList(typeof(MyElement))]
    public class MyList : DList
    {
        protected override DElement NewElement(DList list, int index, object value = null)
        {
            return new MyElement(list, index, value);
        }
    }

    public class MyElement : DElement
    {
        public MyElement(DList list, int index, object value = null) : base(list, index, value) { }
    }

    [RESTar(Methods.GET)]
    public class MyDynamicTable : DDictionary, IDDictionary<MyDynamicTable, MyDynamicTableKvp>
    {
        public MyDynamicTableKvp NewKeyPair(MyDynamicTable dict, string key, object value = null) =>
            new MyDynamicTableKvp(dict, key, value);
    }

    public class MyDynamicTableKvp : DKeyValuePair
    {
        public MyDynamicTableKvp(DDictionary dict, string key, object value = null) : base(dict, key, value) { }
    }

    #endregion

    #endregion
}