using System;
using Starcounter;

namespace RESTarExample.TestDb
{
    [Database]
    public class EmployeeDetails : TestBase
    {
        public int Salary;
        public DateTime DateOfEmployment;
    }
}
