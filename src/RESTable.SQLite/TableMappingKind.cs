namespace RESTable.Sqlite
{
    /// <summary>
    /// The different kinds of RESTable.Sqlite table mappings
    /// </summary>
    public enum TableMappingKind
    {
        /// <summary>
        /// A static declared CLR class bound to an Sqlite table
        /// </summary>
        Static,

        /// <summary>
        /// An elastic declared CLR class (may contain dynamic members), bound to an Sqlite
        /// table with an explicit schema of allowed members.
        /// </summary>
        Elastic
    }
}