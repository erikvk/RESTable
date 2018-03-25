using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using RESTar.Starcounter;
using static RESTar.Admin.Settings;

namespace RESTar.Queries
{
    internal static class NetworkController
    {
        private static INetworkProvider[] Providers { get; set; }

        internal static void AddNetworkBindings(IEnumerable<INetworkProvider> providers)
        {
            Providers = providers.ToArray();
            Providers.ForEach(provider => provider.AddBindings(RESTarConfig.Methods, _Uri, _Port));
        }

        internal static void RemoveNetworkBindings()
        {
            Providers.ForEach(provider => provider.RemoveBindings(RESTarConfig.Methods, _Uri, _Port));
        }
    }
}