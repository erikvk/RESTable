﻿using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;
using RESTar.Linq;

namespace RESTar.Auth
{
    internal class AccessRights : Dictionary<IResource, RESTarMethods[]>
    {
        static AccessRights() => Root = RESTarConfig.Resources
            .ToDictionary(r => r, r => RESTarConfig.Methods)
            .CollectDict(dict => new AccessRights(dict));

        internal static readonly AccessRights Root;

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