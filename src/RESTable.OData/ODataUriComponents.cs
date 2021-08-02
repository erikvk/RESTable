using System.Collections.Generic;
using RESTable.Requests;

namespace RESTable.OData
{
    internal class ODataUriComponents : IUriComponents
    {
        public string ProtocolIdentifier { get; }
        public string? ResourceSpecifier { get; internal set; }
        public string? ViewName { get; internal set; }
        IReadOnlyCollection<IUriCondition> IUriComponents.Conditions => Conditions;
        IReadOnlyCollection<IUriCondition> IUriComponents.MetaConditions => MetaConditions;
        public List<IUriCondition> Conditions { get; }
        public List<IUriCondition> MetaConditions { get; }
        public IMacro? Macro { get; internal set; }

        public IProtocolProvider ProtocolProvider { get; }

        public ODataUriComponents(IProtocolProvider protocolProvider)
        {
            ProtocolProvider = protocolProvider;
            ProtocolIdentifier = protocolProvider.ProtocolIdentifier;
            Conditions = new List<IUriCondition>();
            MetaConditions = new List<IUriCondition>();
        }
    }
}