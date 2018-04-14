using System;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid terminal resource declaration
    /// </summary>
    public class InvalidTerminalDeclaration : RESTarException
    {
        internal InvalidTerminalDeclaration(Type terminal, string info) : base(ErrorCodes.InvalidTerminalDeclaration,
            $"Invalid terminal declaration '{terminal.RESTarTypeName()}'. Terminal types " + info) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid terminal resource declaration
    /// </summary>
    public class InvalidBinaryDeclaration : RESTarException
    {
        internal InvalidBinaryDeclaration(Type binary, string info) : base(ErrorCodes.InvalidBinaryResourceDeclaration,
            $"Invalid binary resource declaration '{binary.RESTarTypeName()}'. Binary resource types " + info) { }
    }
}