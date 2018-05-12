using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using RESTar.Meta;

namespace RESTar.Internal.Auth
{
    internal class AccessRights : Dictionary<IResource, Method[]>
    {
        static AccessRights() => Root = new AccessRights();
        internal static AccessRights Root { get; }
        internal static void ReloadRoot() => RESTarConfig.Resources.ForEach(r => Root[r] = RESTarConfig.Methods);
        private AccessRights() { }

        internal static AccessRights ToAccessRights(IEnumerable<AccessRight> accessRights)
        {
            var ar = new AccessRights();
            foreach (var right in accessRights)
            foreach (var resource in right.Resources)
                ar[resource] = ar.ContainsKey(resource)
                    ? ar[resource].Union(right.AllowedMethods).ToArray()
                    : right.AllowedMethods;
            return ar;
        }

        internal new Method[] this[IResource resource]
        {
            get => TryGetValue(resource, out var r) ? r : null;
            set => base[resource] = value;
        }
    }
}