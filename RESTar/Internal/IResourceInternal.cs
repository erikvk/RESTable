using System.Collections.Generic;

namespace RESTar.Internal
{
    internal interface IResourceInternal
    {
        IReadOnlyList<IEntityResource> InnerResources { get; set; }
        string Description { get; set; }
        IReadOnlyList<Methods> AvailableMethods { get; set; }
    }
}