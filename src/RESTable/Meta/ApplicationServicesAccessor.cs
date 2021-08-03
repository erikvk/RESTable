using RESTable.ContentTypeProviders;

namespace RESTable.Meta
{
    internal static class ApplicationServicesAccessor
    {
        internal static IJsonProvider JsonProvider { get; set; } = null!;
        internal static TypeCache TypeCache { get; set; } = null!;
        internal static ResourceCollection ResourceCollection { get; set; } = null!;
    }
}