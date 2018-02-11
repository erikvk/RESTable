using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    public class NoAvalailableDynamicTable : BadRequest
    {
        internal NoAvalailableDynamicTable() : base(ErrorCodes.NoAvalailableDynamicTable,
            "RESTar have no more unallocated dynamic tables. Remove an existing table and try again.") { }
    }
}