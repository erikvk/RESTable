using System.Collections.Generic;
using RESTable.Meta;

namespace RESTable.Auth;

public class AccessRight
{
    public AccessRight(ICollection<IResource> resources, Method[] allowedMethods)
    {
        Resources = resources;
        AllowedMethods = allowedMethods;
    }

    public ICollection<IResource> Resources { get; }
    public Method[] AllowedMethods { get; }
}