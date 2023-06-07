using System.Collections.Generic;

namespace RESTable.Meta.Internal;

internal interface IResourceInternal
{
    string? Description { set; }
    IReadOnlyCollection<Method> AvailableMethods { set; }
    void AddInnerResource(IResource resource);
    IEnumerable<IResource> GetInnerResources();
}
