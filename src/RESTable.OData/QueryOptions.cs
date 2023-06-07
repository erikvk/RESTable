﻿namespace RESTable.OData;

/// <summary>
///     Valid query options in the RESTable implementation of OData
/// </summary>
internal enum QueryOptions
{
    /// <summary>
    ///     Default
    /// </summary>
    none = default,

    /// <summary>
    ///     $filter | Equivalent to including conditions in request
    /// </summary>
    filter,

    /// <summary>
    ///     $orderby | Equivalent to Order_desc or Order_asc
    /// </summary>
    orderby,

    /// <summary>
    ///     $select | Equivalent to Select
    /// </summary>
    select,

    /// <summary>
    ///     $skip | Equivalent to Offset
    /// </summary>
    skip,

    /// <summary>
    ///     $top | Equivalent to Limit
    /// </summary>
    top,

    /// <summary>
    ///     $search | Equivalent to Search
    /// </summary>
    search
}
