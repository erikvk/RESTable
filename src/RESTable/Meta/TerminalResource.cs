using RESTable.Resources;
using RESTable.Results;

namespace RESTable.Meta
{
    /// <summary>
    /// A static generic class for manually getting RESTable terminal resources by type
    /// </summary>
    public static class TerminalResource<T> where T : class, ITerminal
    {
        /// <summary>
        /// Gets the terminal resource for a given type, and throws an UnknownResource exception 
        /// if there is no such resource
        /// </summary>
        public static ITerminalResource<T> Get => RESTableConfig.ResourceByType.SafeGet(typeof(T)) as ITerminalResource<T>
                                                  ?? throw new UnknownResource(typeof(T).GetRESTableTypeName());


        /// <summary>
        /// Gets the terminal resource for a given type or null if there is no such resource
        /// </summary>
        public static ITerminalResource<T> SafeGet => RESTableConfig.ResourceByType.SafeGet(typeof(T)) as ITerminalResource<T>;

        /// <summary>
        /// Gets the resource specifier for a given terminal resource
        /// </summary>
        public static string ResourceSpecifier => Get.Name;
    }
}