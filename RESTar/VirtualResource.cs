using System;
using System.Collections.Generic;
using System.Reflection;
using Starcounter;
using static RESTar.RESTarOperations;
using static RESTar.RESTarConfig;

namespace RESTar
{
    [Database, RESTar(RESTarPresets.ReadOnly)]
    public class VirtualResource : Resource
    {
        public override int NrOfColumns => Type.GetProperties().Length;

        public override IEnumerable<dynamic> Selector(IRequest request)
            => VrOperations[Type][Select].Invoke((dynamic) request);

        public override void Inserter(IEnumerable<dynamic> entities)
            => VrOperations[Type][Insert].Invoke((dynamic) entities);

        public override void Updater(IEnumerable<dynamic> entities)
            => VrOperations[Type][Update].Invoke((dynamic) entities);

        public override void Deleter(IEnumerable<dynamic> entities)
            => VrOperations[Type][Delete].Invoke((dynamic) entities);

        public VirtualResource(Type type)
        {
            Type = type;
        }
    }
}