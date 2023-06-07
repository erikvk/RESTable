﻿namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Thrown when a request failed resource-specific authentication
///     search string.
/// </summary>
public class FailedResourceAuthentication : Forbidden
{
    /// <inheritdoc />
    internal FailedResourceAuthentication(string info) : base(ErrorCodes.FailedResourceAuthentication, info) { }
}
