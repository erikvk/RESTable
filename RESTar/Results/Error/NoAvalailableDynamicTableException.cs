﻿using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar has run out of dynamic tables for allocation to new 
    /// dynamic resources.
    /// </summary>
    public class NoAvalailableDynamicTableException : BadRequest
    {
        internal NoAvalailableDynamicTableException() : base(ErrorCodes.NoAvalailableDynamicTable,
            "RESTar have no more unallocated dynamic tables. Remove an existing table and try again.") { }
    }
}