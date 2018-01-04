using System;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class InvalidResourceViewDeclaration : RESTarException
    {
        internal InvalidResourceViewDeclaration(Type view, string message) : base(ErrorCodes.InvalidResourceViewDeclaration,
            $"Invalid resource view declaration for view '{view.Name}' in Resource '{view.DeclaringType?.FullName}'. {message}") { }
    }
}