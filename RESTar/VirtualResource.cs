using System;
using Starcounter;

namespace RESTar
{
    [Database, RESTar(RESTarPresets.ReadOnly)]
    public class VirtualResource : Resource
    {
        public override int NrOfColumns => Type.GetProperties().Length;

        public VirtualResource(Type type)
        {
            Type = type;
        }
    }
}
