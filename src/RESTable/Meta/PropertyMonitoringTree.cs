using System;
using System.Collections.Generic;
using RESTable.Meta.Internal;

namespace RESTable.Meta
{
    /// <inheritdoc />
    /// <summary>
    /// Property monitoring trees is a data structure for properties (and properties of properties
    /// etc.) of a given type, along with these properties dependencies on other properties. When 
    /// a change is monitored in some property, a term relative to the root type is generated and
    /// sent to the given callback along with the previous and new values for the changed property.
    /// </summary>
    public class PropertyMonitoringTree : IDisposable
    {
        internal ObservedChangeHandler HandleObservedChange { get; }

        /// <summary>
        /// The component separator to use in output terms from this monitoring tree
        /// </summary>
        public string OutputTermComponentSeparator { get; }

        /// <summary>
        /// The term stub to append all output terms to
        /// </summary>
        public Term Stub { get; }

        /// <summary>
        /// The property links of this monitoring tree
        /// </summary>
        public HashSet<PropertyLink> AllLinks { get; }

        /// <summary>
        /// Creates a new property tree for the given root type
        /// </summary>
        /// <param name="rootType">The root type to monitor</param>
        /// <param name="outputTermComponentSeparator">The component separator to use in output terms</param>
        /// <param name="stub">The term stub to append all output terms to</param>
        /// <param name="handleObservedChange">The handler of output terms and new and old values</param>
        internal PropertyMonitoringTree
        (
            Type rootType,
            string outputTermComponentSeparator,
            Term stub,
            ObservedChangeHandler handleObservedChange,
            TypeCache typeCache
        )
        {
            OutputTermComponentSeparator = outputTermComponentSeparator;
            HandleObservedChange = handleObservedChange;
            Stub = stub;
            AllLinks = new HashSet<PropertyLink>();

            HashSet<Type> discoveredTypes = new();

            void recurseTree(Type owner, PropertyLink? rootWard)
            {
                if (!discoveredTypes.Add(owner)) return;
                if (owner.ImplementsGenericInterface(typeof(IEnumerable<>), out var p))
                {
                    var elementType = p![0];
                    var link = new PropertyLink(this, rootWard, new AnyIndexProperty(elementType, owner));
                    recurseTree(elementType, link);
                }
                foreach (var property in typeCache.GetDeclaredProperties(owner).Values)
                {
                    var link = new PropertyLink(this, rootWard, property);
                    recurseTree(property.Type, link);
                }
            }

            recurseTree(rootType, null);

            Activate();
        }

        /// <summary>
        /// Makes all links active and registers all event listeners
        /// </summary>
        public void Activate()
        {
            foreach (var link in AllLinks)
                link.Activate();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var link in AllLinks)
                link.Dispose();
        }
    }
}