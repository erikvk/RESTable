using System;
using RESTar.Admin;
using RESTar.Deflection;
using RESTar.Operations;
using static RESTar.Deflection.TermBindingRules;

namespace RESTar.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// A non-generic interface for RESTar resource views
    /// </summary>
    public interface IView : ITarget
    {
        /// <summary>
        /// The type of the view, static or dynamic
        /// </summary>
        ViewType ViewType { get; }
    }

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
        public string Name { get; }

        /// <inheritdoc />
        public string Description { get; }

        /// <inheritdoc />
        public Type Type { get; }

        /// <inheritdoc />
        public ViewType ViewType { get; }

        /// <inheritdoc />
        public Selector<T> Select { get; }

        internal View(Type type)
        {
            var attribute = type.GetAttribute<RESTarViewAttribute>();
            ConditionBindingRule = attribute.AllowDynamicConditions
                ? StaticWithDynamicFallback
                : OnlyStatic;
            Name = type.Name;
            Description = attribute.Description;
            Type = type;
            ViewType = ViewType.Static;
            Select = DelegateMaker.GetDelegate<Selector<T>>(type);
        }
    }
}