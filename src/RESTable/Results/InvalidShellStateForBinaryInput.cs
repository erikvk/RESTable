﻿using System.Net;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Thrown from the WebSocket shell when the shell is in an invalid state to handle incoming
///     binary data.
/// </summary>
internal class InvalidShellStateForBinaryInput : Error
{
    internal InvalidShellStateForBinaryInput() : base(ErrorCodes.ShellError, "")
    {
        StatusCode = HttpStatusCode.ServiceUnavailable;
        StatusDescription = "Invalid shell state for binary input";
    }
}
