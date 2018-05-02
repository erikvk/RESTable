using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dynamit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar;
using RESTar.Linq;
using RESTar.Operations;
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

    [Database]
    public class DtTest
    {
        public DateTime DtLocal;
        public DateTime DtUTC;

        public DtTest()
        {
            DtLocal = DateTime.Now;
            DtUTC = DateTime.UtcNow;
        }
    }

    public class Resource1
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
        public JObject MyDict;
    }

    [RESTar]
    public class MyBinaryResource : IBinaryResource<MyBinaryResource>
    {
        public (Stream stream, ContentType contentType) Select(IRequest<MyBinaryResource> request)
        {
            var stream = new MemoryStream();
            using (var swr = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                swr.Write("This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data! I repeat: " +
                          "This is my important binary data! I repeat: This is my important binary data!");
            }
            return (stream, "text/plain");
        }
    }

    [Database]
    public class DbClass
    {
        public string MyString;
        public int MyInt;
    }

    [RESTar]
    public class DbClassWrapper : ResourceWrapper<DbClass>
    {
        [RESTarView]
        public class MyView : ISelector<DbClass>
        {
            public IEnumerable<DbClass> Select(IRequest<DbClass> request)
            {
                return StarcounterOperations<DbClass>.Select(request);
            }
        }
    }

    [RESTar(Method.POST)]
    public class OtherClass : IInserter<OtherClass>
    {
        public string MyString { get; set; }
        public int MyInt { get; set; }

        public int Insert(IRequest<OtherClass> request)
        {
            var k = 0;
            Db.TransactAsync(() =>
            {
                foreach (var i in request.GetInputEntities())
                {
                    new DbClass
                    {
                        MyInt = i.MyInt,
                        MyString = i.MyString
                    };
                    k += 1;
                }
            });
            return k;
        }
    }

    [RESTar(Method.GET)]
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
                return new[] {new Option("Foo", "a foo", strings => { })};
            }
        }
    }

    [Database, RESTar]
    public class MyStarcounterResource
    {
        public string MyString { get; set; }
        public int MyInteger { get; set; }
        public DateTime MyDateTime { get; set; }
        public MyStarcounterResource MyOtherStarcounterResource { get; set; }
    }

    [RESTar]
    public class MyEntityResource : ISelector<MyEntityResource>, IInserter<MyEntityResource>,
        IUpdater<MyEntityResource>, IDeleter<MyEntityResource>
    {
        public string TheString { get; set; }
        public int TheInteger { get; set; }
        public DateTime TheDateTime { get; set; }
        public MyEntityResource TheOtherEntityResource { get; set; }

        /// <summary>
        /// Private properties are not includeded in output and cannot be set in input. 
        /// This property is only used internally to determine DB object identity.
        /// </summary>
        private ulong? ObjectNo { get; set; }

        private static MyEntityResource FromDbObject(MyStarcounterResource dbObject)
        {
            if (dbObject == null) return null;
            return new MyEntityResource
            {
                TheString = dbObject.MyString,
                TheInteger = dbObject.MyInteger,
                TheDateTime = dbObject.MyDateTime,
                TheOtherEntityResource = FromDbObject(dbObject.MyOtherStarcounterResource),
                ObjectNo = dbObject.GetObjectNo()
            };
        }

        private static MyStarcounterResource ToDbObject(MyEntityResource _object)
        {
            if (_object == null) return null;
            var dbObject = _object.ObjectNo is ulong objectNo
                ? Db.FromId<MyStarcounterResource>(objectNo)
                : new MyStarcounterResource();
            dbObject.MyString = _object.TheString;
            dbObject.MyInteger = _object.TheInteger;
            dbObject.MyDateTime = _object.TheDateTime;
            dbObject.MyOtherStarcounterResource = ToDbObject(_object.TheOtherEntityResource);
            return dbObject;
        }

        public IEnumerable<MyEntityResource> Select(IRequest<MyEntityResource> request) => Db
            .SQL<MyStarcounterResource>($"SELECT t FROM {typeof(MyStarcounterResource).FullName} t")
            .Select(FromDbObject)
            .Where(request.Conditions);

        public int Insert(IRequest<MyEntityResource> request) => Db.Transact(() => request
            .GetInputEntities()
            .Select(ToDbObject)
            .Count());

        public int Update(IRequest<MyEntityResource> request) => Db.Transact(() => request
            .GetInputEntities()
            .Select(ToDbObject)
            .Count());

        public int Delete(IRequest<MyEntityResource> request) => Db.Transact(() =>
        {
            var i = 0;
            foreach (var item in request.GetInputEntities())
            {
                item.Delete();
                i += 1;
            }
            return i;
        });
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

    [Database, RESTar]
    public class MyStatic3
    {
        public EE E { get; set; }
        public string Foo { get; set; }
    }

    [RESTar(Method.GET)]
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

    [RESTar(Method.GET)]
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

    [RESTar(Method.GET, AllowDynamicConditions = true)]
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

    [RESTar(Method.GET, Singleton = true)]
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

        [RESTar(Method.GET, Description = "Returns a fine object")]
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
            var entities = request.GetInputEntities();
            return entities.Count();
        }

        public IEnumerable<R> Select(IRequest<R> request)
        {
            return new[] {new R {S = "Swoo", Ss = new[] {"S", "Sd"}}};
        }

        public int Update(IRequest<R> request)
        {
            var entities = request.GetInputEntities();
            return entities.Count();
        }

        public int Delete(IRequest<R> request)
        {
            var entities = request.GetInputEntities();
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

    [RESTar(Method.GET)]
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