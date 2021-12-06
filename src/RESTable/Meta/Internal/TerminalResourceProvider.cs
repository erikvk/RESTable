using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTable.Resources;

namespace RESTable.Meta.Internal;

public class TerminalResourceProvider
{
    public TerminalResourceProvider(TypeCache typeCache, ResourceCollection resourceCollection)
    {
        BuildTerminalMethod = typeof(TerminalResourceProvider).GetMethod
        (
            nameof(MakeTerminalResource),
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        TypeCache = typeCache;
        ResourceCollection = resourceCollection;
    }

    private MethodInfo? BuildTerminalMethod { get; }
    private TypeCache TypeCache { get; }
    private ResourceCollection ResourceCollection { get; }

    public void RegisterTerminalTypes(List<Type> terminalTypes)
    {
        foreach (var type in terminalTypes.OrderBy(t => t.GetRESTableTypeName()))
        {
            var resource = (IResource?) BuildTerminalMethod?.MakeGenericMethod(type).Invoke(this, null);
            if (resource is null)
                throw new Exception($"Could not construct terminal resource for type {type}");
            ResourceCollection.AddResource(resource);
        }
    }

    private IResource MakeTerminalResource<T>() where T : Terminal
    {
        return new TerminalResource<T>(TypeCache);
    }
}