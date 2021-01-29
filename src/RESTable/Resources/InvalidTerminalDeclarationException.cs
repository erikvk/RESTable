using System;

namespace RESTable.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable encounters an invalid terminal resource declaration
    /// </summary>
    public class InvalidTerminalDeclarationException : RESTableException
    {
        internal InvalidTerminalDeclarationException(Type terminal, string message) : base(ErrorCodes.InvalidTerminalDeclaration,
            $"Invalid terminal declaration '{terminal.GetRESTableTypeName()}'. Terminal types {message}") { }
    }
}