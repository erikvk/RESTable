using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot allocate a dynamic table for resource insertion
    /// </summary>
    public class NoAvalailableDynamicTable : BadRequest
    {
        internal NoAvalailableDynamicTable() : base(ErrorCodes.NoAvalailableDynamicTable,
            "RESTar have no more unallocated dynamic tables. Remove an existing table and try again.") { }
    }
}