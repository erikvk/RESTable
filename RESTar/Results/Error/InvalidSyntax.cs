﻿using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a syntax error was discovered when parsing a request
    /// </summary>
    public class InvalidSyntax : BadRequest
    {
        internal InvalidSyntax(ErrorCodes errorCode, string message) : base(errorCode,
            "Syntax error while parsing request: " + message) { }
    }
}