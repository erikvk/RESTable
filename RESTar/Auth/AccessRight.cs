using System.Collections.Generic;
using RESTar.Internal;

namespace RESTar.Auth
{
    internal class AccessRight
    {
        internal ICollection<IResourceView> Resources;
        internal RESTarMethods[] AllowedMethods;
    }
}