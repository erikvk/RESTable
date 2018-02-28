using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;
using RESTar.Linq;
using static RESTar.Internal.Authenticator;

namespace RESTar.Auth
{
    internal class AccessRights : Dictionary<IResource, Methods[]>
    {
        internal string Key { get; }
        internal static AccessRights Root { get; set; }
        private static string RootKey { get; } = Guid.NewGuid().ToString("N");

        internal static string NewRootToken()
        {
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

        internal new Methods[] this[IResource resource]
        {
            get => TryGetValue(resource, out var r) ? r : null;
            set => base[resource] = value;
        }
    }
}