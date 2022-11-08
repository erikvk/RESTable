using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Meta.Internal;
using static RESTable.Method;

namespace RESTable.Auth;

/// <summary>
///     Describes a set of valid operations for a set of resources, assigned to a given token – for example an
///     API key.
/// </summary>
public class AccessRights : ReadOnlyDictionary<IResource, Method[]>
{
    public AccessRights(IRequestAuthenticator? authenticator, string? token, IDictionary<IResource, Method[]> assignments) : base(assignments)
    {
        Authenticator = authenticator;
        Token = token;
    }

    public IRequestAuthenticator? Authenticator { get; }
    public string? Token { get; }

    public new Method[]? this[IResource resource]
    {
        get => TryGetValue(resource, out var r) ? r : null;
        internal set
        {
            if (value is null)
                Dictionary.Remove(resource);
            else Dictionary[resource] = value;
        }
    }

    public static IDictionary<IResource, Method[]> CreateAssignments(IEnumerable<AllowAccess> allowAccessItems, ResourceCollection resourceCollection)
    {
        var assignments = new Dictionary<IResource, Method[]>();

        foreach (var allowAccess in allowAccessItems)
        {
            if (allowAccess.Resources is null)
                throw new NullReferenceException(nameof(allowAccess.Resources));
            if (allowAccess.Methods is null)
                throw new NullReferenceException(nameof(allowAccess.Methods));
            var accessRight = new AccessRight
            (
                allowAccess.Resources.Select(resourceCollection.SafeFindResources)
                    .SelectMany(iresources => iresources.Union(iresources.Cast<IResourceInternal>()
                        .SelectMany(r => r.GetInnerResources())))
                    .OrderBy(r => r.Name)
                    .ToList(),
                GetDistinctMethods(allowAccess.Methods)
                    .OrderBy(i => i, MethodComparer.Instance)
                    .ToArray()
            );
            foreach (var resource in accessRight.Resources)
                assignments[resource] = assignments.ContainsKey(resource)
                    ? assignments[resource].Union(accessRight.AllowedMethods).ToArray()
                    : accessRight.AllowedMethods;
        }

        foreach (var resource in resourceCollection.Where(r => r.GETAvailableToAll))
            if (assignments.TryGetValue(resource, out var methods))
                assignments[resource] = methods.Union(new[] { GET, REPORT, HEAD })
                    .OrderBy(i => i, MethodComparer.Instance)
                    .ToArray();
            else assignments[resource] = new[] { GET, REPORT, HEAD };

        return assignments;
    }

    private static IEnumerable<Method> GetDistinctMethods(IEnumerable<string> methodsArray)
    {
        var methodSet = new HashSet<Method>();
        foreach (var methodItem in methodsArray)
        {
            var method = methodItem.Trim();
            if (method == "*")
                return EnumMember<Method>.Values;
            var parsedMethod = (Method) Enum.Parse(typeof(Method), method, true);
            methodSet.Add(parsedMethod);
        }
        return methodSet;
    }

    internal void Clear()
    {
        Dictionary.Clear();
    }
}
