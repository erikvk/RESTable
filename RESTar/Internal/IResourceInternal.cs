using System.Collections.Generic;

namespace RESTar.Internal
{
    internal interface IResourceInternal
    {
        IReadOnlyList<IResource> InnerResources { get; set; }
        string Description { set; }
        IReadOnlyList<Methods> AvailableMethods { set; }
        void SetAlias(string alias);
    }
}