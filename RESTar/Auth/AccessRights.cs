using System.Collections.Generic;
using RESTar.Linq;
using RESTar.Resources;

namespace RESTar.Auth
{
    internal class AccessRights : Dictionary<IResource, Method[]>
    {
        static AccessRights() => Root = new AccessRights();
        internal static AccessRights Root { get; }
        internal static void ReloadRoot() => RESTarConfig.Resources.ForEach(r => Root[r] = RESTarConfig.Methods);

        internal new Method[] this[IResource resource]
        {
            get => TryGetValue(resource, out var r) ? r : null;
            set => base[resource] = value;
        }
    }
}