using System.Threading.Tasks;

namespace RESTable.Meta;

/// <summary>
///     Represents a setter for a property. This is an open delgate, taking a
///     target object and a value to assign to the property.
/// </summary>
/// <param name="target">The target of the open delegate invocation</param>
/// <param name="value">The value to set the property to</param>
public delegate ValueTask Setter<TOwner, TValue>(TOwner target, TValue? value);
