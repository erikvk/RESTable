using System.Reflection;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot find a property/column in a given resource by a given property name.
    /// </summary>
    public class UnknownProperty : NotFound
    {
        internal UnknownProperty(MemberInfo type, string str) : base(ErrorCodes.UnknownProperty,
            $"Could not find any property in {(type.HasAttribute<RESTarViewAttribute>() ? $"view '{type.Name}' or type '{Resource.Get(type.DeclaringType)?.Name}'" : $"type '{type.Name}'")} by '{str}'.") { }
    }
}