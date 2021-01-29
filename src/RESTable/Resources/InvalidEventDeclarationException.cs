using System;

namespace RESTable.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable encounters an invalid event type declration
    /// </summary>
    public class InvalidEventDeclarationException : RESTableException
    {
        internal InvalidEventDeclarationException(Type eventType, string message) : base(ErrorCodes.InvalidEventDeclaration,
            $"Invalid event resource type declaration for '{eventType.GetRESTableTypeName()}'. Event resource types {message}") { }
    }
}