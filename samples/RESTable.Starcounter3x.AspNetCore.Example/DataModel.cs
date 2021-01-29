using RESTable.Resources;
using Starcounter.Database;

namespace RESTable.Example
{
    [RESTable, Database]
    public abstract class Person
    {
        public abstract string FirstName { get; set; }
        public abstract string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }
}