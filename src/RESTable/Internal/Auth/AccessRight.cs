using System.Collections.Generic;
using RESTable.Meta;

namespace RESTable.Internal.Auth
{
    internal class AccessRight
    {
        internal ICollection<IResource> Resources { get; }
        internal Method[] AllowedMethods { get; }

        public AccessRight(ICollection<IResource> resources, Method[] allowedMethods)
        {
            Resources = resources;
            AllowedMethods = allowedMethods;
        }
    }
}