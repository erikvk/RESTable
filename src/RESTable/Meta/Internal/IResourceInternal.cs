using System.Collections.Generic;

namespace RESTable.Meta.Internal
{
    internal interface IResourceInternal
    {
        void AddInnerResource(IResource resource);
        IEnumerable<IResource> GetInnerResources();
        string Description { set; }
        IReadOnlyCollection<Method> AvailableMethods { set; }
    }
}