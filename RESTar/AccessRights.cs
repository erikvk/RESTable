using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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