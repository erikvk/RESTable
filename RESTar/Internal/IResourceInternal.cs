using System.Collections.Generic;

namespace RESTar.Internal
{
    internal interface IResourceInternal
    {
        IReadOnlyList<IResource> InnerResources { get; set; }
        string Description { get; set; }
        IReadOnlyList<Methods> AvailableMethods { get; set; }
    }
}