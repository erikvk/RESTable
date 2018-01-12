using System;
using RESTar.Deflection;
using RESTar.Operations;
using static RESTar.Deflection.TermBindingRules;

namespace RESTar.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// A non-generic interface for RESTar resource views
    /// </summary>
    public interface IView : ITarget { }

    /// <inheritdoc cref="IView" />
    /// <summary>
    /// Represents a RESTar resource view
    /// </summary>
    public class View<T> : IView, ITarget<T> where T : class
    {
        /// <inheritdoc />
        /// <summary>
        /// The binding rule to use when binding conditions to this view
        /// </summary>
        public TermBindingRules ConditionBindingRule { get; }

        /// <inheritdoc />
        public string FullName { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public string Namespace { get; }

        /// <inheritdoc />
        public string Description { get; }

        /// <inheritdoc />
        public Type Type { get; }

        /// <inheritdoc />
        public Selector<T> Select { get; }

        /// <inheritdoc />
        public WebSocketConnectionHandler WebSocketConnectionHandler { get; }

        internal View(Type type)
        {
            Type = type;
            Namespace = type.Namespace;
            Name = type.Name;
            FullName = type.FullName;
            Select = DelegateMaker.GetDelegate<Selector<T>>(type);
            WebSocketConnectionHandler = DelegateMaker.GetDelegate<WebSocketConnectionHandler>(type);
            var viewAttribute = type.GetAttribute<RESTarViewAttribute>();
            Description = viewAttribute.Description;
            ConditionBindingRule = viewAttribute.AllowDynamicConditions
                ? DeclaredWithDynamicFallback
                : OnlyDeclared;
        }

        /// <inheritdoc />
        public override string ToString() => FullName;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is View<T> view && view.FullName == FullName;

        /// <inheritdoc />
        public override int GetHashCode() => FullName.GetHashCode();
    }
}