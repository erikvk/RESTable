using System.Collections.Generic;
using RESTable.Meta;

namespace RESTable.Auth
{
    /// <summary>
    /// Access rights describing no access, i.e. access to no resources
    /// </summary>
    public class NoAccess : AccessRights
    {
        public NoAccess() : base(null, new Dictionary<IResource, Method[]>()) { }
    }
}