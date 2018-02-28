using System;
using System.Collections.Generic;
using RESTar.Internal;
using RESTar.Linq;

namespace RESTar.Auth
{
    internal class AccessRights : Dictionary<IResource, Methods[]>
    {
        internal static AccessRights Root { get; private set; }
        internal string Key { get; }
        internal static string RootToken { get; } = Guid.NewGuid().ToString("N");
        internal static string RootKey { get; } = Guid.NewGuid().ToString("N");

        internal static void ReloadRoot()
        {
            Root = new AccessRights(RootKey);
            RESTarConfig.Resources.ForEach(r => Root[r] = RESTarConfig.Methods);
            Authenticator.AuthTokens[RootToken] = Root;
        }

        internal AccessRights(string key) => Key = key;

        internal new Methods[] this[IResource resource]
        {
            get => TryGetValue(resource, out var r) ? r : null;
            set => base[resource] = value;
        }
    }
}