using System;
using System.Collections.Generic;
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

        public override void Inserter(IEnumerable<dynamic> entities, IRequest request)
            => VrOperations[Type][Insert].Invoke((dynamic) entities, request);

        public override void Updater(IEnumerable<dynamic> entities, IRequest request)
            => VrOperations[Type][Update].Invoke((dynamic) entities, request);

        public override void Deleter(IEnumerable<dynamic> entities, IRequest request)
            => VrOperations[Type][Delete].Invoke((dynamic) entities, request);

        public VirtualResource(Type type)
        {
            Type = type;
        }
    }
}