using System.Collections.Generic;
using RESTable.Requests;

namespace RESTable.DefaultProtocol;

internal class DefaultProtocolUriComponents : IUriComponents
{
    public DefaultProtocolUriComponents(IProtocolProvider protocolProvider)
    {
        ProtocolIdentifier = protocolProvider.ProtocolIdentifier;
        ProtocolProvider = protocolProvider;
        Conditions = new List<IUriCondition>();
        MetaConditions = new List<IUriCondition>();
    }

    public List<IUriCondition> Conditions { get; }
    public List<IUriCondition> MetaConditions { get; }
    public string ProtocolIdentifier { get; }
    public string? ResourceSpecifier { get; internal set; }
    public string? ViewName { get; internal set; }
    public IMacro? Macro { get; internal set; }
    IReadOnlyCollection<IUriCondition> IUriComponents.Conditions => Conditions;
    IReadOnlyCollection<IUriCondition> IUriComponents.MetaConditions => MetaConditions;
    public IProtocolProvider ProtocolProvider { get; }
}
