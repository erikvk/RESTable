using System.Collections.Generic;
using System.Runtime.Serialization;
using RESTar;
using Starcounter;

namespace RESTarExample.TestDb
{
    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Company : TestBase
    {
        public string Name;
        public Employee CEO;

        [IgnoreDataMember]
        public IEnumerable<Employee> Employees
        {
            get { return Db.SQL<Employee>($"SELECT t FROM {typeof(Employee)} t WHERE t.Company =?", this); }
            set { }
        }
    }
}
