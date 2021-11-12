using RESTable.Resources;

namespace RESTable.Sqlite
{
    /// <inheritdoc />
    /// <summary>
    /// Decorate a class definition with this attribute to register it with 
    /// the Sqlite resource provider.
    /// </summary>
    public sealed class SqliteAttribute : EntityResourceProviderAttribute
    {
        /// <summary>
        /// To manually bind against a certain Sqlite table, set the CustomTableName 
        /// to that table's name.
        /// </summary>
        public string? CustomTableName { get; }

        public SqliteAttribute(string? customTableName = null)
        {
            CustomTableName = customTableName;
        }
    }
}