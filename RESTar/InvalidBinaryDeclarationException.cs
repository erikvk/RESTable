using System;
using RESTar.Internal;

namespace RESTar {
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid terminal resource declaration
    /// </summary>
    public class InvalidBinaryDeclarationException : RESTarException
    {
        internal InvalidBinaryDeclarationException(Type binary, string info) : base(ErrorCodes.InvalidBinaryResourceDeclaration,
            $"Invalid binary resource declaration '{binary.RESTarTypeName()}'. Binary resource types " + info) { }
    }
}