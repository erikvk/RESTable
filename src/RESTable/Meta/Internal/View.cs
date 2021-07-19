using System;
using System.Collections.Generic;
using System.Reflection;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Meta.Internal
{
    /// <inheritdoc cref="IView" />
    /// <summary>
    /// Represents a RESTable resource view
    /// </summary>
    internal class View<TResource> : IView, ITarget<TResource> where TResource : class
    {
        /// <inheritdoc />
        /// <summary>
        /// The binding rule to use when binding conditions to this view
        /// </summary>
        public TermBindingRule ConditionBindingRule { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public string Description { get; }

        /// <inheritdoc />
        public Type Type { get; }

        private AsyncViewSelector<TResource> AsyncViewSelector { get; }
        private ViewSelector<TResource> ViewSelector { get; }

        public IEnumerable<TResource> Select(IRequest<TResource> request) => ViewSelector(request);

        public IAsyncEnumerable<TResource> SelectAsync(IRequest<TResource> request) => AsyncViewSelector(request);

        /// <inheritdoc />
        [RESTableMember(hide: true)]
        public IEntityResource EntityResource { get; private set; }

        public void SetEntityResource(IEntityResource resource) => EntityResource = resource;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }

        internal View(Type viewType, TypeCache typeCache)
        {
            var viewAttribute = viewType.GetCustomAttribute<RESTableViewAttribute>();
            Type = viewType;
            if (viewAttribute is not null)
            {
                Name = viewAttribute.CustomName ?? viewType.Name;
                ViewSelector = DelegateMaker.GetDelegate<ViewSelector<TResource>>(viewType);
                AsyncViewSelector = DelegateMaker.GetDelegate<AsyncViewSelector<TResource>>(viewType);
                Members = typeCache.GetDeclaredProperties(viewType);
                Description = viewAttribute.Description;
                ConditionBindingRule = viewAttribute.AllowDynamicConditions
                    ? TermBindingRule.DeclaredWithDynamicFallback
                    : TermBindingRule.OnlyDeclared;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"{EntityResource.Name}-{Name}";

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is View<TResource> view && view.Name == Name;

        /// <inheritdoc />
        public override int GetHashCode() => Name.GetHashCode();
    }
}