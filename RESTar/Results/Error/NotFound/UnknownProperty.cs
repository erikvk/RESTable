using System.Reflection;
using RESTar.Internal;

namespace RESTar.Results.Error.NotFound
{
    internal class UnknownProperty : NotFound
    {
        internal UnknownProperty(MemberInfo type, string str) : base(ErrorCodes.UnknownProperty,
            $"Could not find any property in {(type.HasAttribute<RESTarViewAttribute>() ? $"view '{type.Name}' or type '{Resource.Get(type.DeclaringType)?.Name}'" : $"type '{type.Name}'")} by '{str}'.") { }
    }
}