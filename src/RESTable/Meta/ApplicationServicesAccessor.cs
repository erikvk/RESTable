using RESTable.ContentTypeProviders;

namespace RESTable.Meta
{
    internal static class ApplicationServicesAccessor
    {
        internal static IJsonProvider JsonProvider { get; set; }
        internal static TypeCache TypeCache { get; set; }
        internal static ResourceCollection ResourceCollection { get; set; }
    }
}