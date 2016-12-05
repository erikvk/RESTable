using System;
using Starcounter;

namespace RESTar.TestDb
{
    [Database]
    public class EmployeeDetails : TestBase
    {
        public int Salary;
        public DateTime DateOfEmployment;
    }
}
