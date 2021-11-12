using System.Collections.Generic;
using RESTable.Meta;

namespace RESTable.Auth
{
    /// <summary>
    /// Access rights describing root access, i.e. access to all resources
    /// </summary>
    public class RootAccess : AccessRights
    {
        private ResourceCollection ResourceCollection { get; }

        public RootAccess(ResourceCollection resourceCollection) : base(null, null, new Dictionary<IResource, Method[]>())
        {
            ResourceCollection = resourceCollection;
            Load();
        }

        internal void Load()
        {
            Clear();
            foreach (var resource in ResourceCollection)
                this[resource] = EnumMember<Method>.Values;
        }
    }
}