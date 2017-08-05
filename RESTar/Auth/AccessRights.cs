using System.Collections.Generic;
using RESTar.Internal;

namespace RESTar.Auth
{
    internal class AccessRights : Dictionary<IResource, RESTarMethods[]>
    {
        internal static AccessRights Root { get; set; }

        internal AccessRights() { }

        internal AccessRights(IDictionary<IResource, RESTarMethods[]> other) : base(other)
        {
        }

        internal new RESTarMethods[] this[IResource resource]
        {
            get => ContainsKey(resource) ? base[resource] : null;
            set => base[resource] = value;
        }
    }
}