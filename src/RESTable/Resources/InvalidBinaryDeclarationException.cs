using System;

namespace RESTable.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable encounters an invalid terminal resource declaration
    /// </summary>
    public class InvalidBinaryDeclarationException : RESTableException
    {
        internal InvalidBinaryDeclarationException(Type binary, string message) : base(ErrorCodes.InvalidBinaryResourceDeclaration,
            $"Invalid binary resource declaration '{binary.GetRESTableTypeName()}'. Binary resource types {message}") { }
    }
}