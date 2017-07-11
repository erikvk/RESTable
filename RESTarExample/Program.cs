using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar;
using RESTar.Internal;
using Starcounter;

// ReSharper disable RedundantExplicitArrayCreation

#pragma warning disable 1591

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

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class MyResource
    {
        public int MyId;
        public string MyMember;
        public string SomeMember;
    }

    public class C
    {
        public string Key;
        public string Operator;
        public dynamic Value;
    }

    [RESTar(RESTarPresets.ReadAndWrite)]
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

    [RESTar(RESTarMethods.GET)]
    public class MyDynamicTable : DDictionary, IDDictionary<MyDynamicTable, MyDynamicTableKvp>,
        ISelector<MyDynamicTable>
    {
        public MyDynamicTableKvp NewKeyPair(MyDynamicTable dict, string key, object value = null) =>
            new MyDynamicTableKvp(dict, key, value);

        public IEnumerable<MyDynamicTable> Select(IRequest request) =>
            DDictionaryOperations<MyDynamicTable>.Select(request);
    }

    public class MyDynamicTableKvp : DKeyValuePair
    {
        public MyDynamicTableKvp(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }
}