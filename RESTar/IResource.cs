using System;
using System.Runtime.Serialization;
using Starcounter;

namespace RESTar
{
    [Database]
    [RESTar(RESTarPresets.ReadOnly)]
    public abstract class Resource
    {
        public string Name;
        public string AvailableMethods => Type.AvailableMethods()?.ToMethodsString();
        
        public abstract int NrOfColumns { get; }

        [IgnoreDataMember]
        public Type Type
        {
            get { return Name.FindResource(); }
            set { Name = value.FullName; }
        }
    }
}