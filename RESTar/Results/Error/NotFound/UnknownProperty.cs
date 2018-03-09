using System.Reflection;
using RESTar.Internal;

namespace RESTar.Results.Error.NotFound
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a property by some search string
    /// </summary>
    public class UnknownProperty : NotFound
    {
        internal UnknownProperty(MemberInfo type, string searchString) : base(ErrorCodes.UnknownProperty,
            $"Could not find any property in {(type.HasAttribute<RESTarViewAttribute>() ? $"view '{type.Name}' or type '{Resource.Get(type.DeclaringType)?.Name}'" : $"type '{type.Name}'")} by '{searchString}'.") { }
    }
}