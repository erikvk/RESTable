using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar;
using RESTar.Internal;
using Starcounter;

namespace RESTarExample
{
    public class Program
    {
        public static void Main()
        {
            RESTarConfig.Init
            (
                requireApiKey: true,
                allowAllOrigins: false,
                viewEnabled: true,
                configFilePath: "C:\\Mopedo\\Mopedo.config",
                setupMenu: true
            );
            TestDatabase.Init();
        }
    }

    public class C
    {
        public string Key;
        public string Operator;
        public dynamic Value;
    }

    [RESTar(RESTarPresets.ReadAndWrite, Viewable = true)]
    public class R : IInserter<R>, ISelector<R>, IUpdater<R>, IDeleter<R>
    {
        public string S { get; set; }
        public string[] Ss { get; set; }

        public int Insert(IEnumerable<R> entities, IRequest request)
        {
            return entities.Count();
        }

        public IEnumerable<R> Select(IRequest request)
        {
            return new R[] {new R {S = "Swoo", Ss = new[] {"S", "Sd"}}};
        }

        public int Update(IEnumerable<R> entities, IRequest request)
        {
            return entities.Count();
        }

        public int Delete(IEnumerable<R> entities, IRequest request)
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

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class MyResource
    {
        public string String;
        public int Integer;
        public DateTime Date;
        public MyOther Other;
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite, Dynamic = true)]
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

    [DDictionary(typeof(MyDynamicTableKvp))]
    public class MyDynamicTable : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new MyDynamicTableKvp(dict, key, value);
        }
    }

    public class MyDynamicTableKvp : DKeyValuePair
    {
        public MyDynamicTableKvp(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [RESTar(RESTarPresets.ReadAndWrite), DDictionary(typeof(MyDynamicTable2Kvp))]
    public class MyDynamicTable2 : DDictionary, ISelector<DDictionary>
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new MyDynamicTable2Kvp(dict, key, value);
        }

        public IEnumerable<DDictionary> Select(IRequest request) => DDictionaryOperations.Select(request);
    }

    public class MyDynamicTable2Kvp : DKeyValuePair
    {
        public MyDynamicTable2Kvp(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }
}