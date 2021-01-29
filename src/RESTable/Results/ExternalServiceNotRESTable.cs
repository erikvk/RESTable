using System;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable found a non-RESTable response for a remote RESTable request
    /// </summary>
    internal sealed class ExternalServiceNotRESTable : NotFound
    {
        internal ExternalServiceNotRESTable(Uri uri, Exception ie = null) : base(ErrorCodes.ExternalServiceNotRESTable,
            $"A remote request was made to '{uri}', but the response was not recognized as a compatible RESTable service response", ie) { }
    }
}