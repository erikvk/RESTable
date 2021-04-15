using RESTable.Linq;
using RESTable.Meta;

namespace RESTable.Internal.Auth
{
    /// <summary>
    /// Access rights describing root access, i.e. access to all resources
    /// </summary>
    public class RootAccess : AccessRights
    {
        private ResourceCollection ResourceCollection { get; }

        public RootAccess(ResourceCollection resourceCollection) : base(null)
        {
            ResourceCollection = resourceCollection;
            Load();
        }

        internal void Load()
        {
            Clear();
            ResourceCollection.ForEach(r => this[r] = EnumMember<Method>.Values);
        }
    }
}