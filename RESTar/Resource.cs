using System;
using System.Linq;
using System.Runtime.Serialization;
using Starcounter;

namespace RESTar
{
    [Database, RESTar(RESTarPresets.ReadOnly)]
    public abstract class Resource
    {
        public string Locator;
        public string AvailableMethods => Type.AvailableMethods()?.ToMethodsString();
        public string Aliases => string.Join(", ", DB.All<ResourceAlias>("Resource", Locator).Select(l => l.Alias));

        [IgnoreDataMember]
        public Type Type
        {
            get { return RESTarConfig.ResourcesDict[Locator.ToLower()]; }
            set { Locator = value.FullName; }
        }
    }
}