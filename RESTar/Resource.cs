using System;
using System.Collections.Generic;
using Starcounter;

namespace RESTar
{
    [Database]
    [RESTar(RESTarPresets.ReadOnly)]
    public abstract class Resource
    {
        public string Name;
        public IEnumerable<RESTarMethods> AvailableMethods => Type.AvailableMethods();
        public IEnumerable<RESTarMethods> BlockedMethods => Type.BlockedMethods();

        [RESTarIgnore]
        public string AvailableMethodsString => Type.AvailableMethods().ToMethodsString();

        [RESTarIgnore]
        public string BlockedMethodsString => Type.BlockedMethods().ToMethodsString();

        [RESTarIgnore]
        public Type Type
        {
            get { return Name.FindResource(); }
            set { Name = value.FullName; }
        }

        public Resource()
        {
            Type = GetType();
        }
    }

    [Database]
    public class ResourceMetaInfo
    {
        
    }
}