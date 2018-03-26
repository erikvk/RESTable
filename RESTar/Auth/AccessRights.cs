using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using RESTar.Resources;
using static RESTar.Auth.Authenticator;

namespace RESTar.Auth
{
    internal class AccessRights : Dictionary<IResource, Method[]>
    {
        internal string Key { get; }
        internal static AccessRights Root { get; private set; }
        private static string RootKey { get; } = Guid.NewGuid().ToString("N");

        internal static string NewRootToken()
        {
            if (Root == null) ReloadRoot();
            var rootToken = Guid.NewGuid().ToString("N");
            AuthTokens[rootToken] = Root;
            return rootToken;
        }

        internal static string NewAuthToken(AccessRights rights)
        {
            var token = Guid.NewGuid().ToString("N");
            AuthTokens[token] = rights;
            return token;
        }

        internal static void ReloadRoot()
        {
            Root = new AccessRights(RootKey);
            RESTarConfig.Resources.ForEach(r => Root[r] = RESTarConfig.Methods);
            AuthTokens.Where(pair => pair.Value.Key == RootKey).ToList().ForEach(pair => AuthTokens[pair.Key] = Root);
        }

        internal AccessRights(string key) => Key = key;

        internal new Method[] this[IResource resource]
        {
            get => TryGetValue(resource, out var r) ? r : null;
            set => base[resource] = value;
        }
    }
}