using System;
using System.Collections.Generic;
using System.Reflection;
using RESTar.Resources;
using RESTar.Resources.Operations;

namespace RESTar.Meta.Internal
{
    /// <inheritdoc cref="IView" />
    /// <summary>
    /// Represents a RESTar resource view
    /// </summary>
    internal class View<T> : IView, ITarget<T> where T : class
    {
        /// <inheritdoc />
        /// <summary>
        /// The binding rule to use when binding conditions to this view
        /// </summary>
        public TermBindingRules ConditionBindingRule { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public string Description { get; }

        /// <inheritdoc />
        public Type Type { get; }

        /// <inheritdoc />
        public Selector<T> Select { get; }

        /// <inheritdoc />
        public IEntityResource EntityResource { get; internal set; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }

        internal View(Type type)
        {
            Type = type;
            Name = type.Name;
            Select = DelegateMaker.GetDelegate<Selector<T>>(type);
            var viewAttribute = type.GetCustomAttribute<RESTarViewAttribute>();
            Members = type.GetDeclaredProperties();
            Description = viewAttribute.Description;
            ConditionBindingRule = viewAttribute.AllowDynamicConditions
                ? TermBindingRules.DeclaredWithDynamicFallback
                : TermBindingRules.OnlyDeclared;
        }

        /// <inheritdoc />
        public override string ToString() => $"{EntityResource.Name}-{Name}";

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is View<T> view && view.Name == Name;

        /// <inheritdoc />
        public override int GetHashCode() => Name.GetHashCode();
    }
}