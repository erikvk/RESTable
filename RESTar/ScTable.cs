using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Starcounter;

namespace RESTar
{
    [Database]
    [RESTar(RESTarPresets.ReadOnly)]
    public class ScTable : IResource
    {
        public string Name;
        public string AvailableMethods => Type.AvailableMethods().ToMethodsString();
        public string BlockedMethods => Type.BlockedMethods().ToMethodsString();
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

        [IgnoreDataMember]
        public Type Type
        {
            get { return Name.FindResource(); }
            set { Name = value.FullName; }
        }

        public ScTable(Type type)
        {
            Type = type;
        }
    }
}