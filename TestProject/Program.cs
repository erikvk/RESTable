using Starcounter;
using System;
using System.Linq;

// ReSharper disable All

namespace TestProject
{
    [Database]
    public class MyClass
    {
        public DateTime? Time { get; set; }
    }

    public class Program
    {
        public static void Main()
        {
            Db.TransactAsync(() =>
            {
                foreach (var myClass in Db.SQL<MyClass>("SELECT t FROM TestProject.MyClass t"))
                    myClass.Delete();
            });

            Db.TransactAsync(() =>
            {
                foreach (var _ in Enumerable.Range(0, 100))
                {
                    new MyClass {Time = null};
                    new MyClass {Time = DateTime.Now};
                }
            });
            var whereNull = Db.SQL<MyClass>("SELECT t FROM TestProject.MyClass t WHERE t.\"Time\" IS NULL");
            var whereNotNull = Db.SQL<MyClass>("SELECT t FROM TestProject.MyClass t WHERE t.\"Time\" IS NOT NULL");

            var totalCount = Db.SQL<long>("SELECT COUNT(t) FROM TestProject.MyClass t").FirstOrDefault();
            var whereNullCount = whereNull.Count();
            var notNullCount = whereNotNull.Count();

            var s = "";
        }
    }
}