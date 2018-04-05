﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Operations;
using RESTar.Reflection;
using RESTar.Reflection.Dynamic;

namespace RESTar.Resources
{
    internal interface IBucketResource : IResource { }

    internal interface IBucketResource<T> : IBucketResource, IResource<T> where T : class
    {
        /// <summary>
        /// Selects binary content from a bucket resource
        /// </summary>
        BinarySelector<T> SelectBinary { get; }
    }

    internal class BucketResource<T> : IResource<T>, IResourceInternal, IBucketResource<T> where T : class
    {
        public string Name { get; }
        public string Description { get; set; }
        public Type Type { get; }
        public TermBindingRules ConditionBindingRule { get; }
        public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }
        public bool Equals(IResource x, IResource y) => x?.Name == y?.Name;
        public int GetHashCode(IResource obj) => obj.Name.GetHashCode();
        public int CompareTo(IResource other) => string.Compare(Name, other.Name, StringComparison.Ordinal);

        public IReadOnlyList<Method> AvailableMethods { get; set; }
        public string Alias { get; private set; }
        public bool IsInternal { get; }
        public bool IsGlobal { get; }
        public bool IsInnerResource { get; }
        public string ParentResourceName { get; }
        public bool GETAvailableToAll { get; }
        public Type InterfaceType { get; }
        Selector<T> ITarget<T>.Select { get; } = null;
        public IReadOnlyList<IResource> InnerResources { get; set; }
        public void SetAlias(string alias) => Alias = alias;
        public ResourceKind ResourceKind { get; }
        public BinarySelector<T> SelectBinary { get; }

        internal BucketResource(BinarySelector<T> binarySelector)
        {
            Name = typeof(T).FullName ?? throw new Exception();
            Type = typeof(T);
            AvailableMethods = new[] {Method.GET};
            IsInternal = false;
            IsGlobal = true;
            var attribute = typeof(T).GetCustomAttribute<RESTarAttribute>();
            InterfaceType = attribute.Interface;
            ResourceKind = ResourceKind.BucketResource;
            ConditionBindingRule = attribute.AllowDynamicConditions
                ? TermBindingRules.DeclaredWithDynamicFallback
                : TermBindingRules.OnlyDeclared;
            Description = attribute.Description;
            SelectBinary = binarySelector;
            Members = typeof(T).GetDeclaredProperties();
            GETAvailableToAll = attribute.GETAvailableToAll;
            if (Name.Contains('+'))
            {
                IsInnerResource = true;
                var location = Name.LastIndexOf('+');
                ParentResourceName = Name.Substring(0, location).Replace('+', '.');
                Name = Name.Replace('+', '.');
            }
        }
    }
}