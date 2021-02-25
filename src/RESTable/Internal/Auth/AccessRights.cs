using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RESTable.Meta;
using RESTable.Linq;

namespace RESTable.Internal.Auth
{
    internal class AccessRights : ReadOnlyDictionary<IResource, Method[]>
    {
        static AccessRights() => Root = new AccessRights(null);
        internal static AccessRights Root { get; }
        internal static void ReloadRoot() => RESTableConfig.Resources.ForEach(r => Root[r] = EnumMember<Method>.Values);
        internal string ApiKey { get; }
        private AccessRights(string apiKey) : base(new Dictionary<IResource, Method[]>()) => ApiKey = apiKey;

        internal static AccessRights ToAccessRights(IEnumerable<AccessRight> accessRights, string apiKeyHash)
        {
            var ar = new AccessRights(apiKeyHash);
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
            set => Dictionary[resource] = value;
        }

        internal void Clear() => Dictionary.Clear();
    }
}