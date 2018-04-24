using System.Collections.Generic;
using System.Runtime.Serialization;
using RESTar;
using Starcounter;

#pragma warning disable 1591

namespace RESTarExample.TestDb
{
    [Database, RESTar]
    public class Employee : TestBase
    {
        public string Name;

        [DataMember(Name = "Details")] public ulong? DetailsObjectNo;
        [DataMember(Name = "Boss")] public ulong? BossObjectNo;
        [DataMember(Name = "Company")] public ulong? CompanyObjectNo;

        [RESTarMember(ignore: true)] public EmployeeDetails Details
        {
            get => DetailsObjectNo.GetReference<EmployeeDetails>();
            set => DetailsObjectNo = value.GetObjectNo();
        }

        [RESTarMember(ignore: true)] public Employee Boss
        {
            get => BossObjectNo.GetReference<Employee>();
            set => BossObjectNo = value.GetObjectNo();
        }

        [RESTarMember(ignore: true)] public Company Company
        {
            get => CompanyObjectNo.GetReference<Company>();
            set => CompanyObjectNo = value.GetObjectNo();
        }

        [RESTarMember(ignore: true)] public IEnumerable<Employee> Subordinates
        {
            get { return Db.SQL<Employee>($"SELECT t FROM {GetType().FullName} t WHERE t.Boss =?", this); }
            set { }
        }
    }
}