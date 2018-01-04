using System;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an error was detected in a resource view declaration.
    /// </summary>
    internal class ResourceViewDeclarationException : RESTarException
    {
        internal ResourceViewDeclarationException(Type view, string message) : base(ErrorCodes.InvalidResourceViewDeclaration,
            $"Invalid resource view declaration for view '{view.Name}' in Resource '{view.DeclaringType?.FullName}'. {message}") { }
    }
}