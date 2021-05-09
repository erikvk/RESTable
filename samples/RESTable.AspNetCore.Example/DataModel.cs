using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.Meta;
using RESTable.Resources;
using RESTable.SQLite;

namespace RESTable.Example
{
    [RESTable, InMemory]
    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public int? DateOfBirth { get; set; }
        public string ObjectID { get; }

        public List<string> Interests { get; }

        private long JobRowId { get; set; }

        public Job Job
        {
            get => SQLite<Job>.Select($"WHERE RowId={JobRowId}").FirstOrDefaultAsync().Result;
            set
            {
                SQLite<Job>.Insert(value).CountAsync().AsTask().Wait();
                JobRowId = value.RowId;
            }
        }

        public Person()
        {
            ObjectID = Guid.NewGuid().ToString();
            Interests = new List<string>();
        }

        public override bool Equals(object obj) => obj is Person other && other.ObjectID == ObjectID;
        public override int GetHashCode() => ObjectID.GetHashCode();
    }
    
    [RESTable, SQLite]
    public class Animal : SQLiteTable
    {
        public string Name { get; set; }
        public int? Age { get; set; }
        public string Sound { get; set; }
    }

    [RESTable, SQLite]
    public class Job : ElasticSQLiteTable
    {
        public string Title { get; set; }
        public int Salary { get; set; }
    }

    [RESTable(Method.GET, Method.PATCH)]
    public class JobController : ElasticSQLiteTableController<JobController, Job> { }

    [RESTable, SQLite]
    public class Toy : ElasticSQLiteTable
    {
        public string Name { get; set; }
    }
    
    [RESTable]
    public class ResourceController : SQLiteResourceController<ResourceController, Toy>{}

    public class MyStringPropertiesResolver : IEntityTypeContractResolver
    {
        public void ResolveContract(EntityTypeContract contract)
        {
            if (contract.EntityType == typeof(string))
            {
                var nextToLastChar = new CustomProperty<string, char?>
                (
                    name: "NextToLastChar",
                    getter: o => o?.ElementAtOrDefault(o.Length - 2)
                );
                contract.Properties.Add(nextToLastChar);
            }
        }
    }

//    [RESTable, Database]
//    public abstract class Superhero
//    {
//        public abstract string Name { get; set; }
//        public abstract bool HasSecretIdentity { get; set; }
//        public abstract string Gender { get; set; }
//        public abstract int? YearIntroduced { get; set; }
//        public abstract DateTime InsertedAt { get; set; }
//
//        public static Superhero Create(IDatabaseContext db)
//        {
//            var instance = db.Insert<Superhero>();
//            instance.InsertedAt = DateTime.Now;
//            return instance;
//        }
//    }
}