using System;
using System.Collections.Generic;

namespace RESTar.Internal
{
    public interface IResource : IOperationsProvider<object>, IEqualityComparer<IResource>, IComparable<IResource>
    {
        string Name { get; }
        bool Editable { get; }
        ICollection<RESTarMethods> AvailableMethods { get; }
        string AvailableMethodsString { get; }
        Type TargetType { get; }
        string Alias { get; }
        long? NrOfEntities { get; }

        IEnumerable<dynamic> Select(IRequest request);
        int Insert(IEnumerable<dynamic> entities, IRequest request);
        int Update(IEnumerable<dynamic> entities, IRequest request);
        int Delete(IEnumerable<dynamic> entities, IRequest request);
    }
}