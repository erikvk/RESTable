using RESTar.Internal.Sc;
using RESTar.Resources;

namespace RESTar.Dynamic
{
    /// <inheritdoc />
    /// <summary>
    /// Describes a dynamic runtime-defined entity resource
    /// </summary>
    [RESTar(Description = description)]
    public sealed class Resource : ResourceController<Resource, DynamitResourceProvider>
    {
        private const string description = "Dynamic runtime-defined entity resources";
    }
}