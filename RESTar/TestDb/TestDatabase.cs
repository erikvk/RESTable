using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter;

namespace RESTar.TestDb
{
    [RESTar(RESTarPresets.ReadAndUpdate)]
    public class TestDatabase : Resource
    {
        public bool Active
        {
            get { return DB.Exists<TestBase>(); }
            set
            {
                if (value)
                    Generator.GenerateTestDatabase();
                else Generator.DeleteTestDatabase();
            }
        }

        internal static void Init()
        {
            Db.Transact(() =>
            {
                foreach (var obj in DB.All<TestDatabase>())
                    obj.Delete();
                new TestDatabase();
            });
        }
    }
}