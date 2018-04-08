using System;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid terminal resource declaration
    /// </summary>
    public class InvalidTerminalDeclaration : Error
    {
        internal InvalidTerminalDeclaration(Type terminal, string info) : base(ErrorCodes.InvalidTerminalDeclaration,
            $"Invalid terminal declaration '{terminal.RESTarTypeName()}'. Terminal types " + info) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid terminal resource declaration
    /// </summary>
    public class InvalidBinaryDeclaration : Error
    {
        internal InvalidBinaryDeclaration(Type binary, string info) : base(ErrorCodes.InvalidBinaryResourceDeclaration,
            $"Invalid binary resource declaration '{binary.RESTarTypeName()}'. Binary resource types " + info) { }
    }
}