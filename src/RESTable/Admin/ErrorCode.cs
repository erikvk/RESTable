using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using static RESTable.Method;

namespace RESTable.Admin;

/// <inheritdoc />
/// <summary>
///     Gets all error codes used by RESTable
/// </summary>
[RESTable(GET, Description = "The error codes used by RESTable.")]
public class ErrorCode : ISelector<ErrorCode>
{
    /// <summary>
    ///     The name of the error
    /// </summary>
    public ErrorCodes Name { get; private set; }

    /// <summary>
    ///     The numeric code of the error
    /// </summary>
    public int Code { get; private set; }

    /// <inheritdoc />
    public IEnumerable<ErrorCode> Select(IRequest<ErrorCode> request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        return EnumMember<ErrorCodes>
            .GetMembers()
            .Select(m => new ErrorCode { Name = m.Value, Code = m.NumericValue });
    }
}
