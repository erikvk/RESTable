using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RESTable.Meta;

namespace RESTable.Client
{
    internal class RemoteResource : IResource
    {
        public string Name { get; }

        public RemoteResource(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
        }

        private static string ErrorMessage([CallerMemberName] string propertyName = "")
        {
            return $"Cannot call {propertyName} on a remote resource";
        }

        #region Fail all other access

        public string Description => throw new InvalidOperationException(ErrorMessage());
        public Type Type => throw new InvalidOperationException(ErrorMessage());
        public TermBindingRule ConditionBindingRule => throw new InvalidOperationException(ErrorMessage());
        public IReadOnlyDictionary<string, DeclaredProperty> Members => throw new InvalidOperationException(ErrorMessage());
        public bool Equals(IResource x, IResource y) => throw new InvalidOperationException(ErrorMessage());
        public int GetHashCode(IResource obj) => throw new InvalidOperationException(ErrorMessage());
        public int CompareTo(IResource other) => throw new InvalidOperationException(ErrorMessage());
        public IReadOnlyCollection<Method> AvailableMethods => throw new InvalidOperationException(ErrorMessage());
        public bool IsInternal => throw new InvalidOperationException(ErrorMessage());
        public bool IsGlobal => throw new InvalidOperationException(ErrorMessage());
        public bool IsInnerResource => throw new InvalidOperationException(ErrorMessage());
        public string ParentResourceName => throw new InvalidOperationException(ErrorMessage());
        public bool GETAvailableToAll => throw new InvalidOperationException(ErrorMessage());
        public Type InterfaceType => throw new InvalidOperationException(ErrorMessage());
        public ResourceKind ResourceKind => throw new InvalidOperationException(ErrorMessage());

        #endregion
    }
}