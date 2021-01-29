using System.Collections.Generic;
using RESTable.Resources;

namespace RESTable.Internal
{
    internal static class EntityResourceProviderController
    {
        internal static readonly IDictionary<string, IEntityResourceProviderInternal> EntityResourceProviders;
        
        static EntityResourceProviderController() => EntityResourceProviders = new Dictionary<string, IEntityResourceProviderInternal>();
    }
}