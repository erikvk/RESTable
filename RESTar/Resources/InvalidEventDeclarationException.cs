using System;
using RESTar.Internal;

namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid event type declration
    /// </summary>
    public class InvalidEventDeclarationException : RESTarException
    {
        internal InvalidEventDeclarationException(Type eventType, string message) : base(ErrorCodes.InvalidEventDeclaration,
            $"Invalid event type declaration for '{eventType.RESTarTypeName()}'. {message}") { }
    }
}