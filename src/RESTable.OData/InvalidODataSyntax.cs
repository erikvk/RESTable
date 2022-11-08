using System;
using RESTable.Results;

namespace RESTable.OData;

internal class InvalidODataSyntax : BadRequest
{
    public InvalidODataSyntax(ErrorCodes errorCode, string info, Exception? ie = null) : base(errorCode, info, ie) { }
}
