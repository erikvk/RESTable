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
        RESTarResourceType ResourceType { get; }
        bool IsDDictionary { get; }
        bool IsDynamic { get; }
        bool DynamicConditionsAllowed { get; }
        bool IsStarcounterResource { get; }
        bool IsViewable { get; }
        bool IsSingleton { get; }
        string AliasOrName { get; }
        Selector<dynamic> Select { get; }
        Inserter<dynamic> Insert { get; }
        Updater<dynamic> Update { get; }
        Deleter<dynamic> Delete { get; }
    }
}