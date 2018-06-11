using System;
using RESTar.Internal;

namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid terminal resource declaration
    /// </summary>
    public class InvalidTerminalDeclarationException : RESTarException
    {
        internal InvalidTerminalDeclarationException(Type terminal, string info) : base(ErrorCodes.InvalidTerminalDeclaration,
            $"Invalid terminal declaration '{terminal.RESTarTypeName()}'. Terminal types " + info) { }
    }
}