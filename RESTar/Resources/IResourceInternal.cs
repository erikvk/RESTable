using System.Collections.Generic;

namespace RESTar.Resources
{
    internal interface IResourceInternal
    {
        IReadOnlyList<IResource> InnerResources { get; set; }
        string Description { set; }
        IReadOnlyCollection<Method> AvailableMethods { set; }
        void SetAlias(string alias);
    }
}