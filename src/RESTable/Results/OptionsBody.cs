using System.Collections.Generic;
using RESTable.Meta;

namespace RESTable.Results;

public class OptionsBody
{
    public OptionsBody(string resource, ResourceKind resourceKind, IEnumerable<Method> methods)
    {
        Resource = resource;
        ResourceKind = resourceKind;
        Methods = methods;
    }

    public string Resource { get; }
    public ResourceKind ResourceKind { get; }
    public IEnumerable<Method> Methods { get; }
}