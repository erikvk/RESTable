using RESTar;
using RESTarExample.TestDb;
using Starcounter;

namespace RESTarExample
{
    [Database, RESTar(RESTarPresets.ReadAndUpdate)]
    public class TestDatabase
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
                    obj?.Delete();
                new TestDatabase();
            });
        }
    }
}