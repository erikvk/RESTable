using System;
using System.Collections.Generic;
using RESTable.ContentTypeProviders;

namespace RESTable.Internal;

public class CachedProtocolProvider
{
    public CachedProtocolProvider(IProtocolProvider protocolProvider)
    {
        ProtocolProvider = protocolProvider;
        InputMimeBindings = new Dictionary<string, IContentTypeProvider>(StringComparer.OrdinalIgnoreCase);
        OutputMimeBindings = new Dictionary<string, IContentTypeProvider>(StringComparer.OrdinalIgnoreCase);
    }

    public IProtocolProvider ProtocolProvider { get; }
    internal IDictionary<string, IContentTypeProvider> InputMimeBindings { get; }
    internal IDictionary<string, IContentTypeProvider> OutputMimeBindings { get; }
    internal IContentTypeProvider DefaultInputProvider { get; set; } = null!;
    internal IContentTypeProvider DefaultOutputProvider { get; set; } = null!;
}
