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
        private static AccessRights _root;
        internal string Key { get; }

        internal static AccessRights Root
        {
            get
            {
                if (_root == null)
                    ReloadRoot();
                return _root;
            }
            private set => _root = value;
        }

        private static string RootKey { get; } = Guid.NewGuid().ToString("N");

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