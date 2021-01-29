using System;

namespace RESTable.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable encounters an invalid resource view declaration
    /// </summary>
    public class InvalidResourceViewDeclarationException : RESTableException
    {
        internal InvalidResourceViewDeclarationException(Type view, string message) : base(ErrorCodes.InvalidResourceViewDeclaration,
            $"Invalid resource view declaration for view '{view.Name}' in resource '{view.DeclaringType?.GetRESTableTypeName()}'. {message}") { }
    }
}