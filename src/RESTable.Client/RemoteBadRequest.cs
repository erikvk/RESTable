﻿using RESTable.Results;

namespace RESTable.Client
{
    /// <inheritdoc />
    /// <summary>
    /// A Bad Request error received from a remote RESTable service 
    /// </summary>
    internal class RemoteBadRequest : BadRequest
    {
        /// <inheritdoc />
        internal RemoteBadRequest(ErrorCodes code) : base(code, null) { }
    }
}