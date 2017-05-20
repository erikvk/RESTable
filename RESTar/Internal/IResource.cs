using System;
using System.Collections.Generic;
using RESTar.Operations;

namespace RESTar.Internal
{
    public interface IResource : IEqualityComparer<IResource>, IComparable<IResource>
    {
        string Name { get; }
        bool Editable { get; }
        RESTarMethods[] AvailableMethods { get; }
        string AvailableMethodsString { get; }
        Type TargetType { get; }
        string Alias { get; }
        long? NrOfEntities { get; }
        bool IsDynamic { get; }
        RESTarResourceType ResourceType { get; }
        bool IsStarcounterResource { get; }
        bool Viewable { get; }
        bool Singleton { get; }
        string AliasOrName { get; }
        Selector<dynamic> Select { get; }
        Inserter<dynamic> Insert { get; }
        Updater<dynamic> Update { get; }
        Deleter<dynamic> Delete { get; }
    }
}