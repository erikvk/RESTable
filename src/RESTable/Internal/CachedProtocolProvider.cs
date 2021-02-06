using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.ContentTypeProviders;
using RESTable.ProtocolProviders;

namespace RESTable.Internal
{
    public class CachedProtocolProvider
    {
        public IProtocolProvider ProtocolProvider { get; }
        internal IDictionary<string, IContentTypeProvider> InputMimeBindings { get; }
        internal IDictionary<string, IContentTypeProvider> OutputMimeBindings { get; }
        internal IContentTypeProvider DefaultInputProvider => InputMimeBindings.FirstOrDefault().Value;
        internal IContentTypeProvider DefaultOutputProvider => OutputMimeBindings.FirstOrDefault().Value;

        public CachedProtocolProvider(IProtocolProvider protocolProvider)
        {
            ProtocolProvider = protocolProvider;
            InputMimeBindings = new Dictionary<string, IContentTypeProvider>(StringComparer.OrdinalIgnoreCase);
            OutputMimeBindings = new Dictionary<string, IContentTypeProvider>(StringComparer.OrdinalIgnoreCase);
        }
    }
}