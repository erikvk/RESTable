﻿using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.Meta.Internal;

namespace RESTable.Meta
{
    /// <inheritdoc />
    /// <summary>
    /// Links a property with its rootward node in the property monitoring tree
    /// </summary>
    public class PropertyLink : IDisposable
    {
        private PropertyMonitoringTree MonitoringTree { get; }
        internal PropertyLink Rootward { get; }
        internal DeclaredProperty Property { get; }
        private Term TermFromRoot { get; }
        private bool HasUnresolvedIndexes { get; }

        internal PropertyLink(PropertyMonitoringTree monitoringTree, PropertyLink rootward, DeclaredProperty property)
        {
            MonitoringTree = monitoringTree;
            monitoringTree.AllLinks.Add(this);
            Rootward = rootward;
            Property = property;
            TermFromRoot = Term.Append(monitoringTree.Stub, GetTermFromRoot(monitoringTree.OutputTermComponentSeparator), false);
            HasUnresolvedIndexes = TermFromRoot.OfType<AnyIndexProperty>().Any();
        }

        private void OnPropertyChanged(DeclaredProperty declaredProperty, object target, dynamic value, dynamic newValue)
        {
            if (target == null) return;
            MonitoringTree.HandleObservedChange
            (
                termRelativeRoot: TermFromRoot,
                oldValue: value,
                newValue: newValue
            );
            foreach (var definesTerm in declaredProperty.DefinesPropertyTerms)
            {
                var definesNewValue = definesTerm.Evaluate
                (
                    target: target,
                    actualKey: out _,
                    parent: out var definesTarget,
                    property: out var definesProperty
                );
                var definesDeclaredProperty = (DeclaredProperty) definesProperty;
                definesDeclaredProperty.NotifyChange
                (
                    target: definesTarget,
                    oldValue: default(UnknownValue),
                    newValue: definesNewValue
                );
            }
        }

        /// <summary>
        /// Activates this property link by registering its event listener
        /// </summary>
        public void Activate() => Property.PropertyChanged += OnPropertyChanged;

        /// <inheritdoc />
        public void Dispose() => Property.PropertyChanged -= OnPropertyChanged;

        private Term GetTermFromRoot(string componentSeparator)
        {
            var currentLink = this;
            var stack = new Stack<DeclaredProperty>();
            do
            {
                stack.Push(currentLink.Property);
                currentLink = currentLink.Rootward;
            } while (currentLink != null);
            return Term.Create(stack, componentSeparator);
        }
    }
}