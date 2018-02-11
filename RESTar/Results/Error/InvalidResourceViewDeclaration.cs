using System;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    public class InvalidResourceViewDeclaration : RESTarError
    {
        internal InvalidResourceViewDeclaration(Type view, string message) : base(ErrorCodes.InvalidResourceViewDeclaration,
            $"Invalid resource view declaration for view '{view.Name}' in Resource '{view.DeclaringType?.FullName}'. {message}") { }
    }
}