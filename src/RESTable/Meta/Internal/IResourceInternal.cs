using System.Collections.Generic;

namespace RESTable.Meta.Internal
{
    internal interface IResourceInternal
    {
        IReadOnlyList<IResource> InnerResources { get; set; }
        string Description { set; }
        IReadOnlyCollection<Method> AvailableMethods { set; }
    }
}