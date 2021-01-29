namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable cannot allocate a dynamic table for resource insertion
    /// </summary>
    internal class NoAvailableDynamicTable : BadRequest
    {
        internal NoAvailableDynamicTable() : base(ErrorCodes.NoAvalailableDynamicTable,
            "RESTable have no more unallocated dynamic tables. Remove an existing table and try again.") { }
    }
}