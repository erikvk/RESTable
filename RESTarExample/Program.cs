using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using Newtonsoft.Json.Linq;
using RESTar;
using RESTar.Admin;
using RESTar.Resources;
using Starcounter;

// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable UnusedMember.Global

#pragma warning disable 1591

namespace RESTarExample
{
    public static class Program
    {
        public static void Main()
        {
            RESTarConfig.Init
            (
                requireApiKey: true,
                allowAllOrigins: false,
                viewEnabled: true,
                configFilePath: @"C:\Mopedo\mopedo3\Mopedo.config",
                lineEndings: LineEndings.Linux
            );
            DatabaseIndex.Register<MyResource>("MyIndeeex", "MyId");
            TestDatabase.Init();
        }
    }

    [RESTar(Methods.GET)]
    public class MyThing : ResourceWrapper<Table>
    {
    }

    [Database]
    public class Table
    {
        public string STR;
        public DateTime? DT;
        public DateTime DT2;
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

        public int Insert(IEnumerable<R> entities, IRequest<R> request)
        {
            return entities.Count();
        }

        public IEnumerable<R> Select(IRequest<R> request)
        {
            return new[] {new R {S = "Swoo", Ss = new[] {"S", "Sd"}}};
        }

        public int Update(IEnumerable<R> entities, IRequest<R> request)
        {
            return entities.Count();
        }

        public int Delete(IEnumerable<R> entities, IRequest<R> request)
        {
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
        public MyElement(DList list, int index, object value = null) : base(list, index, value)
        {
        }
    }

    [RESTar(Methods.GET)]
    public class MyDynamicTable : DDictionary, IDDictionary<MyDynamicTable, MyDynamicTableKvp>
    {
        public MyDynamicTableKvp NewKeyPair(MyDynamicTable dict, string key, object value = null) =>
            new MyDynamicTableKvp(dict, key, value);
    }

    public class MyDynamicTableKvp : DKeyValuePair
    {
        public MyDynamicTableKvp(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }
}