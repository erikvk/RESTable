using System.Collections.Generic;
using RESTar.Internal;

namespace RESTar.Auth
{
    internal class AccessRights : Dictionary<IResource, Methods[]>
    {
        internal static AccessRights Root { get; set; }

        internal AccessRights()
        {
        }

        internal AccessRights(IDictionary<IResource, Methods[]> other) : base(other)
        {
        }

        internal new Methods[] this[IResource resource]
        {
            get => ContainsKey(resource) ? base[resource] : null;
            set => base[resource] = value;
        }
    }
}