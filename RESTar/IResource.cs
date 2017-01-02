using System;
using System.Collections.Generic;
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

        public abstract IEnumerable<dynamic> Selector(IRequest request);
        public abstract void Inserter(IEnumerable<dynamic> entities);
        public abstract void Updater(IEnumerable<dynamic> entities);
        public abstract void Deleter(IEnumerable<dynamic> entities);

        [IgnoreDataMember]
        public Type Type
        {
            get { return Name.FindResource(); }
            set { Name = value.FullName; }
        }
    }
}