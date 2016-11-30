using System;
using System.Collections.Generic;
using System.Linq;
using Starcounter;

namespace RESTar
{
    [Database]
    [RESTar(RESTarPresets.ReadOnly)]
    public class Table : Resource
    {
        public int NrOfColumns => Schema.Count;
        public long NrOfRows => DB.RowCount(Name);

        public IDictionary<string, string> Schema => Type.GetProperties().ToDictionary
        (
            property => property.Name,
            property => property.PropertyType.FullName
        );

        public Table(Type type)
        {
            Type = type;
        }
    }
}