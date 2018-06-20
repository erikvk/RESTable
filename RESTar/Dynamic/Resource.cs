using RESTar.Internal.Sc;
using RESTar.Resources;

namespace RESTar.Dynamic
{
    [RESTar(Description = description)]
    internal sealed class Resource : ResourceController<Resource, DynamitResourceProvider>
    {
        private const string description = "Dynamic runtime-defined entity resources";
    }
}