using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.Results;

namespace RESTable.Meta.Internal;

internal class TerminalResource<T> : IResource<T>, IResourceInternal, ITerminalResource<T> where T : class
{
    internal TerminalResource(TypeCache typeCache)
    {
        Name = typeof(T).GetRESTableTypeName();
        Type = typeof(T);
        AvailableMethods = [Method.GET];
        IsInternal = false;
        IsGlobal = true;
        InnerResources = new List<IResource>();
        var attribute = typeof(T).GetCustomAttribute<RESTableAttribute>();
        InterfaceType = typeof(T).GetRESTableInterfaceType();
        ResourceKind = ResourceKind.TerminalResource;
        (_, ConditionBindingRule) = typeof(T).GetDynamicConditionHandling(attribute);
        Description = attribute?.Description;
        Members = typeCache.GetDeclaredProperties(typeof(T));
        Constructor = typeof(T).GetConstructors().First();
        ConstructorParameterIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        ConstructorParameterInfos = Constructor.GetParameters();
        for (var i = 0; i < ConstructorParameterInfos.Length; i += 1)
        {
            HasParameterizedConstructor = true;
            var parameter = ConstructorParameterInfos[i];
            var parameterName = parameter.RESTableParameterName(typeof(T).IsDictionary(out _, out _));
            ConstructorParameterIndexes[parameterName] = i;
        }
        GETAvailableToAll = attribute?.GETAvailableToAll == true;
        IsDynamicTerminal = typeof(IDictionary<string, object?>).IsAssignableFrom(typeof(T));

        var typeName = typeof(T).FullName;
        if (typeName?.Contains('+') == true)
        {
            IsInnerResource = true;
            var location = typeName.LastIndexOf('+');
            ParentResourceName = typeName[..location].Replace('+', '.');
            Name = typeName.Replace('+', '.');
        }
        else
        {
            ParentResourceName = null!;
        }
    }

    private bool IsDynamicTerminal { get; }
    private ConstructorInfo Constructor { get; }
    private Dictionary<string, int> ConstructorParameterIndexes { get; }
    private ParameterInfo[] ConstructorParameterInfos { get; }
    private bool HasParameterizedConstructor { get; }

    private List<IResource> InnerResources { get; }
    public string Name { get; }
    public Type Type { get; }
    public IReadOnlyCollection<Method> AvailableMethods { get; set; }
    public bool IsInternal { get; }
    public bool IsGlobal { get; }
    public bool IsInnerResource { get; }
    public string? ParentResourceName { get; }

    public bool Equals(IResource? x, IResource? y)
    {
        return x?.Name == y?.Name;
    }

    public int GetHashCode(IResource obj)
    {
        return obj.Name.GetHashCode();
    }

    public int CompareTo(IResource? other)
    {
        return string.Compare(Name, other?.Name, StringComparison.Ordinal);
    }

    public TermBindingRule ConditionBindingRule { get; }
    public string? Description { get; set; }
    public bool GETAvailableToAll { get; }
    public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }
    public Type? InterfaceType { get; }
    public ResourceKind ResourceKind { get; }

    public IAsyncEnumerable<T> SelectAsync(IRequest<T> request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException();
    }

    public void AddInnerResource(IResource resource)
    {
        InnerResources.Add(resource);
    }

    public IEnumerable<IResource> GetInnerResources()
    {
        return InnerResources.AsReadOnly();
    }

    public override string ToString()
    {
        return Name;
    }

    public override bool Equals(object? obj)
    {
        return obj is TerminalResource<T> t && t.Name == Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public async Task<Terminal> CreateTerminal(RESTableContext context, CancellationToken webSocketCancellationToken, IEnumerable<Condition<T>>? assignments = null)
    {
        var assignmentList = assignments?.ToList() ?? new List<Condition<T>>();

        Terminal newTerminal;
        try
        {
            newTerminal = HasParameterizedConstructor
                ? InvokeParameterizedConstructor(context, webSocketCancellationToken, assignmentList)
                : (Terminal) Constructor.Invoke(null);
        }
        catch (TargetInvocationException tie)
        {
            // An exception occured in the constructor
            throw tie.InnerException ?? tie;
        }

        foreach (var assignment in assignmentList)
        {
            if (assignment.Operator != Operators.EQUALS)
                throw new BadConditionOperator(this, assignment.Operator);
            if (!Members.TryGetValue(assignment.Key, out var property))
            {
                if (newTerminal is IDictionary<string, object?> dynTerminal)
                    dynTerminal[assignment.Key] = assignment.Value;
                else throw new UnknownProperty(Type, this, assignment.Key);
            }
            else
            {
                await property.SetValue(newTerminal, assignment.Value).ConfigureAwait(false);
            }
        }
        if (newTerminal is T terminal and IValidator<T> validator)
        {
            var invalidMembers = validator.GetInvalidMembers(terminal, context).ToList();
            if (invalidMembers.Count > 0) throw new InvalidInputEntity(invalidMembers);
        }
        newTerminal.SetTerminalResource(this);
        return newTerminal;
    }

    private bool TryResolveNonConditionValue(RESTableContext context, Type parameterType, CancellationToken webSocketCancellationToken, out object? value)
    {
        switch (parameterType)
        {
            case var _ when parameterType.IsAssignableFrom(typeof(RESTableContext)):
                value = context;
                return true;
            case var _ when parameterType.IsAssignableFrom(typeof(Headers)):
                value = context.WebSocket!.Headers;
                return true;
            case var _ when parameterType == typeof(CancellationToken):
                value = webSocketCancellationToken;
                return true;
            case var _ when context.GetService(parameterType) is { } resolvedService:
                value = resolvedService;
                return true;
            default:
                value = null;
                return false;
        }
    }

    private Terminal InvokeParameterizedConstructor(RESTableContext context, CancellationToken webSocketCancellationToken, List<Condition<T>> assignmentList)
    {
        var constructorParameterList = new object[ConstructorParameterInfos.Length];
        var parameterAssignments = new Dictionary<int, object?>();
        var missingParameterAssignments = default(List<ParameterInfo>);

        for (var i = 0; i < assignmentList.Count; i += 1)
        {
            var assignment = assignmentList[i];
            if (ConstructorParameterIndexes.TryGetValue(assignment.Key, out var index))
            {
                if (assignment.Operator != Operators.EQUALS)
                    throw new BadConditionOperator(this, assignment.Operator);
                parameterAssignments[index] = assignment.Value;
                assignmentList.RemoveAt(i);
                i -= 1;
            }
        }

        for (var i = 0; i < ConstructorParameterInfos.Length; i += 1)
            if (parameterAssignments.TryGetValue(i, out var value))
            {
                constructorParameterList[i] = value!;
            }
            else
            {
                var parameterInfo = ConstructorParameterInfos[i];
                if (TryResolveNonConditionValue(context, parameterInfo.ParameterType, webSocketCancellationToken, out value))
                {
                    constructorParameterList[i] = value!;
                }
                else if (parameterInfo.IsOptional)
                {
                    constructorParameterList[i] = Missing.Value;
                }
                else
                {
                    missingParameterAssignments ??= new List<ParameterInfo>();
                    missingParameterAssignments.Add(parameterInfo);
                }
            }

        if (missingParameterAssignments?.Count > 0)
        {
            var invalidMembers = missingParameterAssignments.ToInvalidMembers(Type);
            throw new MissingTerminalParameter(Type, invalidMembers);
        }

        return (Terminal) Constructor.Invoke(constructorParameterList);
    }
}
