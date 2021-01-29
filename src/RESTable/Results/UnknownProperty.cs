using System.Reflection;
using RESTable.Meta;
using RESTable.Resources;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable cannot locate a property by some search string
    /// </summary>
    internal class UnknownProperty : NotFound
    {
        internal UnknownProperty(MemberInfo type, string searchString) : base(ErrorCodes.UnknownProperty,
            $"Could not find any property in {(type.HasAttribute<RESTableViewAttribute>() ? $"view '{type.Name}' or type '{Resource.Get(type.DeclaringType)?.Name}'" : $"type '{type.Name}'")} by '{searchString}'.") { }
    }
}