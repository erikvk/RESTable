using Starcounter;
using System;

namespace RESTar
{
    [Database, RESTar(RESTarPresets.ReadOnly)]
    public class VirtualResource : Resource
    {
        public VirtualResource(Type type)
        {
            Type = type;
        }
    }
}
