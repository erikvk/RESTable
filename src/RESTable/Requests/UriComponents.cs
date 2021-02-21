using System.Collections.Generic;
using System.Linq;
using RESTable.ProtocolProviders;

namespace RESTable.Requests
{
    /// <inheritdoc />
    /// <summary>
    /// Encodes read and writable URI components
    /// </summary>
    public class UriComponents : IUriComponents
    {
        /// <inheritdoc />
        public string ResourceSpecifier { get; }

        /// <inheritdoc />
        public string ViewName { get; }

        /// <summary>
        /// The read and writable conditions list
        /// </summary>
        public List<IUriCondition> Conditions { get; }

        /// <summary>
        /// The read and writable meta-conditions list
        /// </summary>
        public List<IUriCondition> MetaConditions { get; }

        /// <inheritdoc />
        public IProtocolProvider ProtocolProvider { get; }

        /// <inheritdoc />
        public IMacro Macro { get; }

        /// <inheritdoc />
        IReadOnlyCollection<IUriCondition> IUriComponents.Conditions => Conditions;

        /// <inheritdoc />
        IReadOnlyCollection<IUriCondition> IUriComponents.MetaConditions => MetaConditions;

        public UriComponents(string resourceSpecifier, string viewName, IEnumerable<IUriCondition> conditions,
            IEnumerable<IUriCondition> metaConditions, IProtocolProvider protocolProvider, IMacro macro)
        {
            ResourceSpecifier = resourceSpecifier;
            ViewName = viewName;
            Conditions = conditions.ToList();
            MetaConditions = metaConditions.ToList();
            ProtocolProvider = protocolProvider;
            Macro = macro;
        }

        internal UriComponents(IUriComponents existing)
        {
            ResourceSpecifier = existing.ResourceSpecifier;
            ViewName = existing.ViewName;
            Conditions = existing.Conditions.ToList();
            MetaConditions = existing.MetaConditions.ToList();
            ProtocolProvider = existing.ProtocolProvider;
            Macro = existing.Macro;
        }

        /// <inheritdoc />
        public override string ToString() => this.ToUriString();
    }
}