using RESTable.NetworkProviders;
using RESTable.Linq;
using static RESTable.Admin.Settings;
using static RESTable.RESTableConfig;

namespace RESTable.Internal
{
    internal static class NetworkController
    {
        private static INetworkProvider[] Providers { get; set; }
        internal static void RemoveNetworkBindings() => Providers?.ForEach(provider => provider?.RemoveRoutes(Methods, _Uri, _Port));

        internal static void AddNetworkBindings(INetworkProvider[] providers)
        {
            Providers = providers;
            Providers.ForEach(provider => provider.AddRoutes(Methods, _Uri, _Port));
        }
    }
}