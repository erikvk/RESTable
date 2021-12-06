using System;

namespace RESTable.Resources;

/// <inheritdoc />
/// <summary>
///     Thrown when RESTable encounters an error with an external resource provider
/// </summary>
public class InvalidEntityResourceProviderException : RESTableException
{
    internal InvalidEntityResourceProviderException(Type providerType, string message) : base(ErrorCodes.EntityResourceProviderError,
        $"An error was found in the declaration of entity resource provider '{providerType.GetRESTableTypeName()}': {message}") { }
}