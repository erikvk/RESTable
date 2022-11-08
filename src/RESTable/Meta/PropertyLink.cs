using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.Meta.Internal;

namespace RESTable.Meta;

/// <inheritdoc />
/// <summary>
///     Links a property with its rootward node in the property monitoring tree
/// </summary>
public class PropertyLink : IDisposable
{
    internal PropertyLink(PropertyMonitoringTree monitoringTree, PropertyLink? rootward, DeclaredProperty property)
    {
        MonitoringTree = monitoringTree;
        monitoringTree.AllLinks.Add(this);
        Rootward = rootward;
        Property = property;
        TermFromRoot = Term.Append(monitoringTree.Stub, GetTermFromRoot(monitoringTree.OutputTermComponentSeparator), false);
        HasUnresolvedIndexes = TermFromRoot.OfType<AnyIndexProperty>().Any();
    }

    private PropertyMonitoringTree MonitoringTree { get; }
    internal PropertyLink? Rootward { get; }
    internal DeclaredProperty Property { get; }
    private Term TermFromRoot { get; }
    private bool HasUnresolvedIndexes { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        Property.PropertyChanged -= OnPropertyChanged!;
    }

    private async void OnPropertyChanged(DeclaredProperty declaredProperty, object? target, dynamic value, dynamic newValue)
    {
        if (target is null) return;
        MonitoringTree.HandleObservedChange
        (
            termRelativeRoot: TermFromRoot,
            oldValue: value,
            newValue: newValue
        );
        foreach (var definesTerm in declaredProperty.DefinesPropertyTerms)
        {
            var termValue = await definesTerm.GetValue(target).ConfigureAwait(false);
            var definesTarget = termValue.Parent;
            var definesProperty = termValue.Property;
            var definesNewValue = termValue.Value;
            var definesDeclaredProperty = (DeclaredProperty?) definesProperty;
            definesDeclaredProperty?.NotifyChange
            (
                definesTarget!,
                default(UnknownValue),
                definesNewValue
            );
        }
    }

    /// <summary>
    ///     Activates this property link by registering its event listener
    /// </summary>
    public void Activate()
    {
        Property.PropertyChanged += OnPropertyChanged!;
    }

    private Term GetTermFromRoot(string componentSeparator)
    {
        var currentLink = this;
        var stack = new Stack<DeclaredProperty>();
        do
        {
            stack.Push(currentLink.Property);
            currentLink = currentLink.Rootward;
        } while (currentLink is not null);
        return Term.Create(stack, componentSeparator);
    }
}
