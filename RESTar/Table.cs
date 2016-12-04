using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Starcounter;

namespace RESTar
{
    [Database]
    [RESTar(RESTarPresets.ReadOnly)]
    public class Table : Resource
    {
        public int NrOfColumns => Schema.Count;
        public long NrOfRows => DB.RowCount(Name);

        public IDictionary<string, string> Schema
        {
            get
            {
                var properties = Type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                var dict = new Dictionary<string, string>();
                foreach (var property in properties)
                {
                    if (!property.HasAttribute<IgnoreDataMemberAttribute>())
                    {
                        var alias = property.GetAttribute<DataMemberAttribute>()?.Name;
                        dict[alias ?? property.Name] = property.PropertyType.FullName;
                    }
                }
                return dict;
            }
        }

        public Table(Type type)
        {
            Type = type;
        }
    }
}