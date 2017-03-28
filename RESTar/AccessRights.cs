using System.Collections.Generic;
using RESTar.Internal;

namespace RESTar
{
    internal class AccessRights : Dictionary<IResource, ICollection<RESTarMethods>>
    {
        internal new ICollection<RESTarMethods> this[IResource resource]
        {
            get { return ContainsKey(resource) ? base[resource] : null; }
            set { base[resource] = value; }
        }
    }

    internal class AccessRight
    {
        internal ICollection<IResource> Resources;
        internal ICollection<RESTarMethods> AllowedMethods;
    }
}