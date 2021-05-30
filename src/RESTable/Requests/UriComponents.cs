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
        public string ProtocolIdentifier { get; }

        /// <inheritdoc />
        public string ResourceSpecifier { get; }

        /// <inheritdoc />
        public string? ViewName { get; }

        /// <summary>
        /// The read and writable conditions list
        /// </summary>
        public List<IUriCondition> Conditions { get; }

        /// <summary>
        /// The read and writable meta-conditions list
        /// </summary>
        public List<IUriCondition> MetaConditions { get; }

        /// <inheritdoc />
        public IMacro Macro { get; }

        public IProtocolProvider ProtocolProvider { get; }

        /// <inheritdoc />
        IReadOnlyCollection<IUriCondition> IUriComponents.Conditions => Conditions;

        /// <inheritdoc />
        IReadOnlyCollection<IUriCondition> IUriComponents.MetaConditions => MetaConditions;

        public UriComponents
        (
            IProtocolProvider protocolProvider,
            string resourceSpecifier,
            string? viewName,
            IEnumerable<IUriCondition> conditions,
            IEnumerable<IUriCondition> metaConditions,
            IMacro macro
        )
        {
            ProtocolIdentifier = protocolProvider.ProtocolIdentifier;
            ProtocolProvider = protocolProvider;
            ResourceSpecifier = resourceSpecifier;
            ViewName = viewName;
            Conditions = conditions.ToList();
            MetaConditions = metaConditions.ToList();
            Macro = macro;
        }

        internal UriComponents(IUriComponents existing)
        {
            ProtocolProvider = null!;
            ProtocolIdentifier = existing.ProtocolIdentifier;
            ResourceSpecifier = existing.ResourceSpecifier;
            ViewName = existing.ViewName;
            Conditions = existing.Conditions.ToList();
            MetaConditions = existing.MetaConditions.ToList();
            Macro = existing.Macro;
        }

        /// <inheritdoc />
        public override string ToString() => this.ToUriString();
    }
}