using System.Collections.Generic;
using System.Runtime.Serialization;
using RESTar;
using Starcounter;

namespace RESTarExample.TestDb
{
    [Database, RESTar(RESTarPresets.ReadAndPrivateWrite)]
    public class Employee : TestBase
    {
        public string Name;
        public EmployeeDetails Details;

        [IgnoreDataMember]
        public Employee Boss
        {
            get { return DbHelper.FromID(BossObjectNo.GetValueOrDefault()) as Employee; }
            set { BossObjectNo = value.GetObjectNo(); }
        }

        [DataMember(Name = "Boss")]
        public ulong? BossObjectNo;

        [IgnoreDataMember]
        public Company Company
        {
            get { return DbHelper.FromID(CompanyObjectNo.GetValueOrDefault()) as Company; }
            set { CompanyObjectNo = value.GetObjectNo(); }
        }

        [DataMember(Name = "Company")]
        public ulong? CompanyObjectNo;

        [IgnoreDataMember]
        public IEnumerable<Employee> Subordinates
        {
            get { return Db.SQL<Employee>($"SELECT t FROM {GetType().FullName} t WHERE t.Boss =?", this); }
            set { }
        }
    }
}
