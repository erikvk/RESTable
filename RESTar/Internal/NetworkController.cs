using System.Collections.Generic;
using System.Linq;
using RESTar.Admin;
using RESTar.Linq;

namespace RESTar.Internal
{
    internal static class NetworkController
    {
        private static INetworkProvider[] Providers { get; set; }

        internal static void AddNetworkBindings(IEnumerable<INetworkProvider> providers)
        {
            Providers = providers.ToArray();
            Providers.ForEach(provider => provider.AddBindings(RESTarConfig.Methods, Settings._Uri, Settings._Port));
        }

        internal static void RemoveNetworkBindings()
        {
            Providers.ForEach(provider => provider.RemoveBindings(RESTarConfig.Methods, Settings._Uri, Settings._Port));
        }
    }
}