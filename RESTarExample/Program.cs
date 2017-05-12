using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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
                configFilePath: "C:\\Mopedo\\Mopedo.config"
            );
            TestDatabase.Init();

            // Activate the test database by setting the Active flag 
            // to true using a PATCH request over the REST API to the
            // RESTarExample.TestDatabase resource.
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite, Visible = true)]
    public class MyResource : IInserter<MyResource>
    {
        public string Str;
        public int Inte;

        public int Insert(IEnumerable<MyResource> entities, IRequest request)
        {
            var count = 0;
            foreach (var entity in entities)
            {
                if (entity.Str == "ASD ASD")
                    throw new Exception("Invalid string");
                count += 1;
            }
            return count;
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite, Dynamic = true)]
    public class MyOther
    {
        [DataMember(Name = "Swoo")] public string Str;
        private MyDynamicTable _ext;

        public MyDynamicTable Ext
        {
            get { return _ext; }
            set
            {
                _ext?.Delete();
                _ext = value;
            }
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