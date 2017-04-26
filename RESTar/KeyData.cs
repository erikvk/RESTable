using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RESTar
{
    internal class KeyData
    {
        internal string RegularKey;
        internal string DatabaseKey;
        internal Type ParentType;
        internal Type PropertyType;
        internal List<PropertyInfo> PropertyChain;
        internal bool DynamicMember;

        internal string FinalName;

        public KeyData(string name, bool dynamicMember = false)
        {
            RegularKey = DatabaseKey = FinalName = name;
            PropertyType = null;
            ParentType = null;
            PropertyChain = null;
            DynamicMember = dynamicMember;
        }

        public KeyData(PropertyInfo property, bool dynamicMember = false)
        {
            RegularKey = property.GetDataMemberName() ?? property.Name;
            DatabaseKey = property.Name;
            FinalName = RegularKey;
            PropertyType = property.PropertyType;
            ParentType = property.DeclaringType;
            PropertyChain = new List<PropertyInfo> {property};
            DynamicMember = dynamicMember;
        }

        public KeyData(string name, Type type, Type parentType, bool dynamicMember = false)
        {
            RegularKey = DatabaseKey = name;
            PropertyType = type;
            ParentType = parentType;
            PropertyChain = null;
            FinalName = null;
            DynamicMember = dynamicMember;
        }

        public KeyData(List<PropertyInfo> propertyChain, string finalName, Type propertyType)
        {
            RegularKey = string.Join(".", propertyChain.Select(prop => prop.GetDataMemberNameOrName()))
                         + "." + finalName;
            DatabaseKey = string.Join(".", propertyChain.Select(prop => prop.Name)) + "." + finalName;
            FinalName = finalName;
            PropertyType = propertyType;
            ParentType = propertyChain.Last().PropertyType;
            PropertyChain = propertyChain;
            DynamicMember = false;
        }

        public KeyData(List<PropertyInfo> propertyChain)
        {
            RegularKey = string.Join(".", propertyChain.Select(prop => prop.GetDataMemberNameOrName()));
            DatabaseKey = string.Join(".", propertyChain.Select(prop => prop.Name));
            FinalName = null;
            PropertyType = propertyChain.Last().PropertyType;
            ParentType = propertyChain.ElementAt(propertyChain.Count - 2).PropertyType;
            PropertyChain = propertyChain;
            DynamicMember = false;
        }
    }
}