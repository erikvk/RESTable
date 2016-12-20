using System;
using System.Collections.Generic;
using System.Reflection;
using Starcounter;

namespace RESTar
{
    [Database, RESTar(RESTarPresets.ReadOnly)]
    public class VirtualResource : Resource
    {
        public override int NrOfColumns => Type.GetProperties().Length;

        public override IEnumerable<dynamic> Getter(IRequest request) => (IEnumerable<dynamic>)
            Type.GetMethod("Get", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] {request});

        public override void Inserter(IEnumerable<dynamic> entities) =>
            Type.GetMethod("Insert", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, new object[] {entities});

        public override void Updater(IEnumerable<dynamic> entities) =>
            Type.GetMethod("Update", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, new object[] { entities });

        public override void Deleter(IEnumerable<dynamic> entities) =>
            Type.GetMethod("Delete", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, new object[] {entities});

        public VirtualResource(Type type)
        {
            Type = type;
        }
    }
}