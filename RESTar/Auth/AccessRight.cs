using System.Collections.Generic;
using RESTar.Resources;

namespace RESTar.Auth
{
    internal class AccessRight
    {
        internal ICollection<IResource> Resources;
        internal Method[] AllowedMethods;
    }
}