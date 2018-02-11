﻿using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error with an external resource provider
    /// </summary>
    public class InvalidExternalResourceProvider : RESTarError
    {
        internal InvalidExternalResourceProvider(string message) : base(ErrorCodes.ResourceProviderError,
            "An error was found in an external ResourceProvider: " + message) { }
    }
}