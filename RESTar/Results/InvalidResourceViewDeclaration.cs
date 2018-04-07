using System;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid resource view declaration
    /// </summary>
    public class InvalidResourceViewDeclaration : Error
    {
        internal InvalidResourceViewDeclaration(Type view, string message) : base(ErrorCodes.InvalidResourceViewDeclaration,
            $"Invalid resource view declaration for view '{view.Name}' in Resource '{view.DeclaringType?.RESTarTypeName()}'. {message}") { }
    }
}