using System;
using RESTar.Deflection;
using RESTar.Operations;

namespace RESTar.Internal
{
    /// <summary>
    /// Describes the most general object of RESTar. Resources and views are ITargets
    /// </summary>
    public interface ITarget
    {
        /// <summary>
        /// The full name of the target
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// The name of the target
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The namespace of the target
        /// </summary>
        string Namespace { get; }

        /// <summary>
        /// Descriptions are visible in the AvailableMethods resource
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The target type
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// The binding rule to use when binding condition terms for this target
        /// </summary>
        TermBindingRules ConditionBindingRule { get; }

        /// <summary>
        /// The action to perform when a WebSocket is connected to this target
        /// </summary>
        WebSocketConnectionHandler WebSocketConnectionHandler { get; }
    }

    /// <inheritdoc />
    /// <summary>
    /// The most general object of RESTar, with a generic parameter describing the 
    /// target type.
    /// </summary>
    public interface ITarget<T> : ITarget where T : class
    {
        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
        Selector<T> Select { get; }
    }
}