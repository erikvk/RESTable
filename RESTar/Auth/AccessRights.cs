using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;

namespace RESTar.Auth
{
    internal class AccessRights : Dictionary<IResourceView, RESTarMethods[]>
    {
        static AccessRights() => Root = RESTarConfig.Resources
            .ToDictionary(r => r, r => RESTarConfig.Methods)
            .CollectDict(dict => new AccessRights(dict));

        internal static readonly AccessRights Root;

        internal AccessRights() { }

        internal AccessRights(IDictionary<IResourceView, RESTarMethods[]> other) : base(other)
        {
        }

        internal new RESTarMethods[] this[IResourceView resource]
        {
            get => ContainsKey(resource) ? base[resource] : null;
            set => base[resource] = value;
        }
    }
}