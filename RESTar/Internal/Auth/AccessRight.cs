using System.Collections.Generic;
using RESTar.Meta;

namespace RESTar.Internal.Auth
{
    internal class AccessRight
    {
        internal ICollection<IResource> Resources;
        internal Method[] AllowedMethods;
    }
}