using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter;

namespace RESTar.TestDb
{
    public static class Generator
    {
        public static Response DeleteTestDatabase()
        {
            foreach (var o in DB.All<TestBase>())
                Db.Transact(() => o.Delete());

            return new Response
            {
                StatusDescription = "Test database deleted"
            };
        }

        public static Response GenerateTestDatabase()
        {
            DeleteTestDatabase();

            var givenNames = new List<string>
            {
                "John",
                "Mary",
                "Carl",
                "Liza",
                "Bernard",
                "Emily",
                "Donald",
                "Carol",
                "Tim",
                "Jane",
                "Peter",
                "Lois"
            };

            var surnames = new List<string>
            {
                "Simpson",
                "Anderson",
                "Marquez",
                "Adams",
                "Jones",
                "Stevens",
                "Sullivan",
                "Arnolds",
                "Smith",
                "Williams",
                "Lewis",
                "Lee"
            };

            var c1 = Db.Transact(() => new Company {Name = "BS Industries"});
            var rand = new Random();
            var ceo = Db.Transact(() =>
            {
                var _ceo = new Employee
                {
                    Name = givenNames[rand.Next(0, 11)] + " " + surnames[rand.Next(0, 11)],
                    Details = new EmployeeDetails
                    {
                        DateOfEmployment = DateTime.Now.AddDays(-rand.Next(15, 3000)),
                        Salary = rand.Next(90000, 110000)
                    },
                    Company = c1
                };
                c1.CEO = _ceo;
                return _ceo;
            });

            foreach (var i in Enumerable.Range(0, 15))
            {
                Db.Transact(() =>
                {
                    var b = new Employee
                    {
                        Name = givenNames[rand.Next(0, 11)] + " " + surnames[rand.Next(0, 11)],
                        Details = new EmployeeDetails
                        {
                            DateOfEmployment = DateTime.Now.AddDays(-rand.Next(15, 3000)),
                            Salary = rand.Next(60000, 70000)
                        },
                        Boss = ceo,
                        Company = c1
                    };

                    foreach (var k in Enumerable.Range(0, rand.Next(6, 24)))
                    {
                        new Employee
                        {
                            Name = givenNames[rand.Next(0, 11)] + " " + surnames[rand.Next(0, 11)],
                            Details = new EmployeeDetails
                            {
                                DateOfEmployment = DateTime.Now.AddDays(-rand.Next(15, 3000)),
                                Salary = rand.Next(40000, 50000)
                            },
                            Company = c1,
                            Boss = b
                        };
                    }
                });
            }

            return new Response
            {
                StatusDescription = "Test database created. New entities now available in resorces " +
                                    "'Employee' and 'Company'"
            };
        }
    }
}