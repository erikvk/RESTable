namespace RESTable
{
    /// <summary>
    /// The levels of metadata that can be used by the Metadata resource
    /// </summary>
    public enum MetadataLevel
    {
        /// <summary>
        /// Only resource lists (EntityResources and TerminalResources) are populated
        /// </summary>
        OnlyResources,

        /// <summary>
        /// Resource lists and type lists (including members) are populated. Type lists cover the 
        /// entire type tree (types used in resource types, etc.)
        /// </summary>
        Full
    }
}